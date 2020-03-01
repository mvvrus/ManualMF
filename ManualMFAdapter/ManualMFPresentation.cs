using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;


namespace ManualMF
{
    class ManualMFPresentation: IAdapterPresentationForm
    {
        
        FormMode m_FormMode;
        AccessDeniedReason m_Reason;
        String m_ErrorMessage;
        //Normal form - ask the user to inform an operator and wait for a decision
        public ManualMFPresentation() { m_FormMode = FormMode.NormalForm;}
        public ManualMFPresentation(FormMode aFormMode) { m_FormMode = aFormMode;}
        public ManualMFPresentation(AccessDeniedReason Reason) { m_FormMode = FormMode.DeniedForm; m_Reason = Reason;}
        public ManualMFPresentation(string ErrorMessage) { m_FormMode = FormMode.ErrorForm; m_ErrorMessage = ErrorMessage; }
        // public const String AUTHBUTTON = "OKButton";
        //Returns an input form part of the authetication page
        //TEMP: just return a form with a single button
        public string GetFormHtml(int lcid)
        {
            HtmlFragmentSupplier supplier = HtmlFragmentSupplier.GetFragmentSupplier(lcid);
            //TODO 
            switch (m_FormMode)
            {
                case FormMode.DeniedForm:
                    return supplier.GetFragment(m_FormMode, m_Reason);
                case FormMode.ErrorForm:
                    return supplier.GetFragment(m_FormMode, m_ErrorMessage);
                default:
                    return supplier.GetFragment(m_FormMode);
            }

            /*
            //No:Do not specify hidden input AuthMetod event this is requred in documentation
            const string REQUIRED_HIDDEN = "<input id=\"authMethod\" type=\"hidden\" name=\"AuthMethod\" value=\"%AuthMethod%\"> <input id=\"context\" type=\"hidden\" name=\"Context\" value=\"%Context%\">";
            //const string REQUIRED_HIDDEN = "<input id=\"context\" type=\"hidden\" name=\"Context\" value=\"%Context%\">";
            if (m_FormMode == FormMode.FinalClose)
            {
                return @"Press Close button to close this window or enter another site address to continue browsing.<br>"+
                  @"<button type=""button"" onclick=""window.open('', '_self', ''); window.close();"">Close</button>";

            }
            else
            //TODO Implement forms for: AlreadyAuthForm, DeniedForm, AlreadyAuthForm, ErrorForm
            {
                return "<form method=\"post\" id=\"authForm\">" + REQUIRED_HIDDEN + "<input id=\"submitButton\" type=\"submit\" name=\"" + AUTHBUTTON + "\" value=\"Auth\"><input id=\"signoutButton\" type=\"submit\" name=\"SignOut\" value=\"Sign Out\"></form>";
                //TODO: Implement real input form
            }
            */ 
        }

        //Returns content to be included into the <HEAD> section of the authetication page
        public string GetFormPreRenderHtml(int lcid)
        {
            //We have nothing to include here
            return null;
        }

        //Returns a title of the authetication page
        public string GetPageTitle(int lcid)
        {
            return "Manual MFA Adapter";
            //TODO: load string to return from a resource
        }
    }
}
