using ManMFOperator.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using ManMFOperator.Controllers;
using System.Resources;

namespace ManMFOperator
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Configuration.InitConfiguration();
            AreaRegistration.RegisterAllAreas();
            DependencyResolver.SetResolver(new DependencyResolverDecorator(DependencyResolver.Current,
                new DependencyResolverDecorator.TypeCreator[] { 
                    new DependencyResolverDecorator.TypeCreator {aType = typeof(HomeController), 
                    Creator = () => new HomeController(UserRepositoryCreator.Create()) }
                    ,new DependencyResolverDecorator.TypeCreator {aType=typeof(IDirectoryAccessor),
                    Creator = ADCachedAccessorCreator.Create}
                    ,new DependencyResolverDecorator.TypeCreator {aType=typeof(IDBAccessor),
                    Creator = ()=>new DBAccessor()}
                }
            ));

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}