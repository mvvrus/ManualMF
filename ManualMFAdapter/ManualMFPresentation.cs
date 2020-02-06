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
        //Returns an input form part of the authetication page
        //TEMP: just return a form with a single button
        public string GetFormHtml(int lcid)
        {
            const string REQUIRED_HIDDEN = "<input id=\"authMethod\" type=\"hidden\" name=\"AuthMethod\" value=\"%AuthMethod%\"> <input id=\"context\" type=\"hidden\" name=\"Context\" value=\"%Context%\">";
            return "<form method=\"post\" id=\"authForm\">" + REQUIRED_HIDDEN + "<input id=\"submitButton\" type=\"submit\" name=\"Submit\" value=\"Auth\"></form>";
            //TODO: Implement real input form
            throw new NotImplementedException();
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
