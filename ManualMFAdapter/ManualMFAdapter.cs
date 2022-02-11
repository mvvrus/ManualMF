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
    static class  AuthState { 
        internal const int AuthPending=1; 
        internal const int AlreadyAuthenticated=2;
        internal const int ProcessingTerminated = 3;
    };

    public class ManualMFAdapter : IAuthenticationAdapter, IAuthenticationAdapterExtension
    {
        //Context propertiy names for IAuthenticationContext.Dictionary
        //UPN - User's principal name from first stage (via claim)
        const String UPN="UPN";
        //STATE - current state of authentication state machine
        const String STATE = "STATE";
        //TOKEN - access tocken controlling access for checking
        const String TOKEN = "TOKEN";

        //Possible STATE values
        

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
        IPAddress ExtractOriginalIP(IHttpRequestParams Request)
        {
            // Return value from X-MS-Forwarded-Client-IP header (proxy connection)  or request.RemoteEndPoint.Address value (direct connection)
            //See if X-MS-Forwarded-Client-IP header present and get its value
            String[] OriginalIPs = Request.Headers.GetValues("X-MS-Forwarded-Client-IP");
            if (null == OriginalIPs || 0==OriginalIPs.Length) //Direct connection
                return Request.RemoteAddress;
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
            return BeginAuthentication(IdentityClaim, new HttpListenerRequestParams(Request), Context);
        }
        
        public IAdapterPresentation BeginAuthentication(Claim IdentityClaim, IHttpRequestParams Request, IAuthenticationContext Context)

        {
            //Perform context initialization
            IPAddress access_from = ExtractOriginalIP(Request);
            String upn = IdentityClaim.Value;
            Context.Data.Add(UPN, upn);
            AccessState auth_state;
            int? token;
            using (SqlConnection conn = new SqlConnection(Configuration.DBConnString))
            {
                conn.Open();
                using (AccessValidator validator = new AccessValidator(conn))
                {

                    //Check current permission state in database, 
                    auth_state = validator.CheckAndRequest(upn, access_from, DateTime.Now.AddMinutes(Configuration.DecisionTimeMinutes),out token); 
                }
                conn.Close();
            }
            //Save access token gotten for later checks
            Context.Data.Add(TOKEN, token);
            //Change state of authentication machine according to the permission state and select appropriate HTML fragment to be shown
            switch (auth_state)
            {
                case AccessState.Allowed:
                    Context.Data[STATE]=AuthState.AlreadyAuthenticated;
                    return new ManualMFPresentation(FormMode.AlreadyAuthForm);
                case AccessState.Denied:
                    //Handle recently denied case
                    Context.Data[STATE] = AuthState.ProcessingTerminated;
                    return new ManualMFPresentation(AccessDeniedReason.DeniedByOperator);
                case AccessState.Pending:
                    //Handle request that was just made or not processed by operator yet
                    Context.Data[STATE] = AuthState.AuthPending;
                    return new ManualMFPresentation(FormMode.NormalForm, token, upn);
                default:
                    //Something went wrong, or else we never get here
                    throw new Exception("Invalid request state from the database:"); //TODO - insert auth_state value into the message
            }
 
        }

        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext Context, IProofData ProofData, HttpListenerRequest Request, out Claim[] Claims)
        {
            return TryEndAuthentication(Context, ProofData, new HttpListenerRequestParams(Request), out Claims);
        }

        public IAdapterPresentation TryEndAuthentication(IAuthenticationContext Context, IProofData ProofData, IHttpRequestParams Request, out Claim[] Claims)
        {
            Claims = null;
            AccessState acc_state;
            AccessDeniedReason deny_reason=AccessDeniedReason.UnknownOrNotDenied;
            int auth_state = (int)Context.Data[STATE];
            String upn = (String)Context.Data[UPN];
            int? token = null;
            switch (auth_state) {
                case AuthState.AlreadyAuthenticated: //Authentication was successful already
                //Trust it, because Context comes from data encrypted the same way as AD FS tokens are
                    acc_state = AccessState.Allowed;
                    break; 
                case AuthState.ProcessingTerminated: //Safeguard agaist processing already terminated request
                default: //Authentication was pending last time
                    //Get required infomation to check/cancel database record
                    IPAddress access_from = ExtractOriginalIP(Request);
                    if (ProofData.Properties.ContainsKey(HtmlFragmentSupplier.CancelButtonName))
                    {
                        //Cancel request: clear request record wich was cancelled while pending
                        using(SqlConnection conn = new SqlConnection(Configuration.DBConnString))
                        {
                            conn.Open();
                            using (AccessValidator validator = new AccessValidator(conn)) validator.Cancel(upn, access_from);
                            conn.Close();
                        }
                        Context.Data[STATE] = auth_state = AuthState.ProcessingTerminated;
                    }
                    //Check for already terminated request (due to user cancels the request or already denied condition)
                    if (AuthState.ProcessingTerminated == auth_state ) 
                    {   //If so, leave the authentication pipeline 
                        ClearContext(Context); //Dispose all context objects
                        return new ManualMFPresentation(FormMode.FinalCloseForm); //show them the form, from which they never return to the pipeline
                    }
                    //If we are here we must check authentication in the database
                    //Check current permission state in the database
                    using(SqlConnection conn = new SqlConnection(Configuration.DBConnString))
                    {
                        conn.Open();
                        using (AccessValidator validator = new AccessValidator(conn))
                        {
                            AccessStateAndReason acc_state_reason = validator.Check(upn, access_from);
                            acc_state = acc_state_reason.State;
                            deny_reason = acc_state_reason.Reason;
                            token = acc_state_reason.Token;
                            //Check access token
                            int? saved_token = (int?)Context.Data[TOKEN];
                            if (token!=null && !token.Equals(saved_token))
                            {
                                //Deny access if check failed
                                acc_state = AccessState.Denied;
                                deny_reason = AccessDeniedReason.InvalidToken;
                            }
                        }
                        conn.Close();
                    }
                    break;
            }
            //Process current authentication state
            switch (acc_state)
            {
                case AccessState.Pending: //Tell them that they should wait more
                    return new ManualMFPresentation(FormMode.WaitMoreForm, token, upn);
                case AccessState.Denied: //Found that authentication was denied
                    Context.Data[STATE] = AuthState.ProcessingTerminated; //Set authentication state to get out of the pipeline next time we return to this method
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
            return OnError(new HttpListenerRequestParams(request),ex);
        }


        public IAdapterPresentation OnError(IHttpRequestParams request, ExternalAuthenticationException ex)
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
            //Implement configuration data initialization 
            if (configData.Data != null) Configuration.ReadConfiguration(configData.Data);
        }

        public void OnAuthenticationPipelineUnload()
        {
            //Currently, we have nothing to do here
        }

    }
}
