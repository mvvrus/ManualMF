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
        public enum FormMode { Normal, AutoLogout };
        private FormMode m_FormMode;
        public ManualMFPresentation() { m_FormMode = FormMode.Normal; }
        public ManualMFPresentation(FormMode aFormMode) { m_FormMode = aFormMode; }
        public const String AUTHBUTTON = "OKButton";
        //Returns an input form part of the authetication page
        //TEMP: just return a form with a single button
        public string GetFormHtml(int lcid)
        {
            //No:Do not specify hidden input AuthMetod event this is requred in documentation
            const string REQUIRED_HIDDEN = "<input id=\"authMethod\" type=\"hidden\" name=\"AuthMethod\" value=\"%AuthMethod%\"> <input id=\"context\" type=\"hidden\" name=\"Context\" value=\"%Context%\">";
            //const string REQUIRED_HIDDEN = "<input id=\"context\" type=\"hidden\" name=\"Context\" value=\"%Context%\">";
            if (m_FormMode == FormMode.AutoLogout)
            {
                return @"You are denied to access this resource.<br>"+
                  @"<button type=""button"" onclick=""window.open('', '_self', ''); window.close();"">Close</button>";

            }
            else
            {
                return "<form method=\"post\" id=\"authForm\">" + REQUIRED_HIDDEN + "<input id=\"submitButton\" type=\"submit\" name=\"" + AUTHBUTTON + "\" value=\"Auth\"><input id=\"signoutButton\" type=\"submit\" name=\"SignOut\" value=\"Sign Out\"></form>";
                //TODO: Implement real input form
            }
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
