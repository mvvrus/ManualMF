using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;
using System.Security.Claims;
using System.Net;
using System.Data.SqlClient;

namespace ManualMF
{
    public class ManualMFAdapter: IAuthenticationAdapter
    {
        //Context propertiy names for IAuthenticationContext.Dictionary
        //UPN - User's principal name from first stage (via claim)
        const String UPN="UPN";
        //CONNMGR - the source of database connection used for the authentication
        const String CONNMGR = "CONNMGR";
        //STATE - current state of authentication state machine
        const String STATE = "STATE";

        //Possible STATE values
        enum AuthState {AuthPending, AlreadyAuthenticated, AlreadyDenied};

        //Dispose and remove any objects stored  in the authentication context Data dictionary
        void ClearContext(IAuthenticationContext Context)
        {
            foreach (var key in Context.Data) {
                IDisposable disp = key.Value as IDisposable;
                if (disp != null) disp.Dispose();
            }
            Context.Data.Clear();
        }

        //Exctract original client IP address from an HTTP request
        IPAddress ExtractOriginalIP(HttpListenerRequest Request)
        {
            // Return value from X-MS-Forwarded-Client-IP header (proxy connection)  or request.RemoteEndPoint.Address value (direct connection)
            //See if X-MS-Forwarded-Client-IP header present and get its value
            String[] OriginalIPs = Request.Headers.GetValues("X-MS-Forwarded-Client-IP");
            if (null == OriginalIPs || 0==OriginalIPs.Length) //Direct connection
                return Request.RemoteEndPoint.Address;
            else //Connection via Web Application Proxy
                return IPAddress.Parse(OriginalIPs[0]);
        }

        /*
         * Authentication pipeline part of IAuthenticationAdapter implementation
        */
        public bool IsAvailableForUser(Claim IdentityClaim, IAuthenticationContext Context)
        {
            //Currently, all users are allowed for authentication
            return true;
        }

        public IAdapterPresentation BeginAuthentication(Claim IdentityClaim, HttpListenerRequest Request, IAuthenticationContext Context)
        {
            //Perform context initialization
            ConnManager connmgr = ConnManager.GetNewManager();
            try {
                Context.Data.Add(CONNMGR, connmgr);
            }
            catch (Exception ex) {
                connmgr.Dispose();
                throw ex;
            }
            IPAddress access_from = ExtractOriginalIP(Request);
            String upn = IdentityClaim.Value;
            Context.Data.Add(UPN, upn);
            AccessState auth_state;
            try
            {
                SqlConnection conn = connmgr.Acquire();
                using (AccessValidator validator = new AccessValidator(conn))
                {

                    //Check current permission state in database, 
                    auth_state = validator.CheckAndRequest(upn, access_from, DateTime.Now.AddMinutes(15)); //TODO Change ValidUntil argement no to use a "magic" constant
                }
            }
            finally
            {
                connmgr.Release();
            }
            //Change state of authentication machine according to the permission state and select appropriate HTML fragment to be shown
            switch (auth_state)
            {
                case AccessState.Allowed:
                    Context.Data[STATE]=AuthState.AlreadyAuthenticated;
                    return new ManualMFPresentation(FormMode.AlreadyAuthForm);
                case AccessState.Denied:
                    //Handle recently denied case
                    Context.Data[STATE] = AuthState.AlreadyDenied;
                    return new ManualMFPresentation(AccessDeniedReason.DeniedByOperator);
                case AccessState.Pending:
                    //Handle request that was just made or not processed by operator yet
                    Context.Data[STATE] = AuthState.AuthPending;
                    return new ManualMFPresentation();
                default:
                    //Something went wrong, or else we never get here
                    throw new Exception("Invalid request state from the database:"); //TODO - insert auth_state value into the message
            }
 
        }

        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext Context, IProofData ProofData, HttpListenerRequest Request, out Claim[] Claims)
        {
            Claims = null;
            AccessState acc_state;
            AccessDeniedReason deny_reason=AccessDeniedReason.UnknownOrNotDenied;
            AuthState auth_state = (AuthState)Context.Data[STATE];
            switch (auth_state) {
                case AuthState.AlreadyAuthenticated: //Authentication was successful already
                    acc_state = AccessState.Allowed;
                    break; 
                case AuthState.AlreadyDenied: //Authentication was denied already
                default: //Authentication was pending last time
                    ConnManager connmgr = (ConnManager)Context.Data[CONNMGR];
                    //Get required infomation to check/cancel database record
                    IPAddress access_from = ExtractOriginalIP(Request);
                    String upn = (String)Context.Data[UPN];
                    if (ProofData.Properties.ContainsKey(HtmlFragmentSupplier.CancelButtonName))
                    {
                        //Cancel request: clear request record wich was cancelled while pending
                        try
                        {
                            SqlConnection conn = connmgr.Acquire();
                            using (AccessValidator validator = new AccessValidator(conn)) validator.Cancel(upn, access_from);
                        }
                        finally
                        {
                            connmgr.Release();
                        }
                    }
                    //Check for cancel request too along with already denied condition
                    if (AuthState.AlreadyDenied == auth_state || ProofData.Properties.ContainsKey(HtmlFragmentSupplier.CancelButtonName)) //Left temporary cancel check implementation for a while
                    {   //If so, leave the authrntication pipeline 
                        ClearContext(Context); //Dispose all context objects
                        return new ManualMFPresentation(FormMode.FinalClose); //show them the form, from which they never return to the pipeline
                    }
                    //If we are here we must check authentication in the database
                    //Check current permission state in the database
                    try
                    {
                        SqlConnection conn = connmgr.Acquire();
                        using (AccessValidator validator = new AccessValidator(conn))
                        {
                            AccessStateAndReason acc_state_reason = validator.Check(upn, access_from);
                            acc_state = acc_state_reason.State;
                            deny_reason = acc_state_reason.Reason;
                        }
                    }
                    finally
                    {
                        connmgr.Release();
                    }
                    break;
            }
            //Process current authentication state
            switch (acc_state)
            {
                case AccessState.Pending: //Tell them that they should wait more
                    return new ManualMFPresentation(FormMode.WaitMoreForm);
                case AccessState.Denied: //Found that authentication was denied
                    Context.Data[STATE] = AuthState.AlreadyDenied; //Set authentication state to get out of the pipeline next time we return to this method
                    return new ManualMFPresentation(deny_reason); //Notify user that his access was denied and why
                case AccessState.Allowed: //authentication was successful
                    ClearContext(Context); ////Dispose all context objects
                    //Required by caller: set authentiction method claim to the value of our method
                    Claims = new[] { new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod", ManualMFMetadata.AUTH_METHOD) };
                    return null; //Inform the caller that the authentication succeded
                default:
                    //Something went wrong, or else we could never get here
                    throw new Exception("Invalid request state from the database:"); //TODO - insert auth_state value into the message
            }

        }

        public IAdapterPresentation OnError(HttpListenerRequest request, ExternalAuthenticationException ex)
        {
            //Just retturn presentation object, that creates HTML fragment for error message
            return new ManualMFPresentation(ex.Message);
        }

        public IAuthenticationAdapterMetadata Metadata
        {
            get { return new ManualMFMetadata(); }
        }

        public void OnAuthenticationPipelineLoad(IAuthenticationMethodConfigData configData)
        {
            //TODO Implement configuration data initialization 
        }

        public void OnAuthenticationPipelineUnload()
        {
            //Currently, we have nothing to do here
        }

    }
}
