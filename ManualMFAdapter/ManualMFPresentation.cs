using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityServer.Web.Authentication.External;
using System.Text.RegularExpressions;


namespace ManualMF
{
    class ManualMFPresentation: IAdapterPresentationForm
    {
        
        FormMode m_FormMode;
        AccessDeniedReason m_Reason;
        String m_ErrorMessage;
        int? m_EPAccessToken;
        //Normal form - ask the user to inform an operator and wait for a decision
        public ManualMFPresentation(FormMode aFormMode) { m_FormMode = aFormMode;}
        public ManualMFPresentation(FormMode aFormMode, int? EPAccessToken) { m_FormMode = aFormMode; m_EPAccessToken = EPAccessToken;}
        public ManualMFPresentation(AccessDeniedReason Reason) { m_FormMode = FormMode.DeniedForm; m_Reason = Reason; }
        public ManualMFPresentation(string ErrorMessage) { m_FormMode = FormMode.ErrorForm; m_ErrorMessage = ErrorMessage; }

        //Returns an input form part of the authetication page
        public string GetFormHtml(int lcid)
        {
            HtmlFragmentSupplier supplier = HtmlFragmentSupplier.GetFragmentSupplier(lcid);
            switch (m_FormMode)
            {
                case FormMode.DeniedForm:
                    return supplier.GetFragment(m_FormMode, m_Reason);
                case FormMode.ErrorForm:
                    return supplier.GetFragment(m_FormMode, m_ErrorMessage);
                case FormMode.NormalForm:
                case FormMode.WaitMoreForm:
                    return supplier.GetFragment(m_FormMode,m_EPAccessToken);
                default:
                    return supplier.GetFragment(m_FormMode);
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
