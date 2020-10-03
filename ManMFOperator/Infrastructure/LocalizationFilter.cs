using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace ManMFOperator.Infrastructure
{
    //This filter changes languge and regional settings used for request processing 
    //to the first language in the list of accepted laguages sent by the client (if any)
    //It isn't intended to be used via controller class/action method attributes, so any attribute functionality is not implemented
    public class LocalizationFilter: IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            //Do nothing after an action
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Select client language before an action 
            String[] user_languages = filterContext.HttpContext.Request.UserLanguages;
            if (user_languages != null && user_languages.Length > 0)
            {   //The client has specified the supported language/regional stetting
                //Set preferred language/regional settings for the processing (current) thread
                CultureInfo clientCulture = CultureInfo.GetCultureInfo(user_languages[0]);
                Thread.CurrentThread.CurrentCulture = clientCulture;
                Thread.CurrentThread.CurrentUICulture = clientCulture;
            }

        }
    }
}