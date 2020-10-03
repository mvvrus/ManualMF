using System.Web;
using System.Web.Mvc;
using ManMFOperator.Infrastructure;

namespace ManMFOperator
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LocalizationFilter()); //To change language settongs to the preferred client setting before processing
        }
    }
}