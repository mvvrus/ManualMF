using System;
using System.Web;
using System.Web.Routing;


namespace AccessWaiterEP
{
    public class RouteHandler: IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new APIHandler();
        }
    }
}