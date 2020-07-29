using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AccessWaiterEP.Infrastructure;

namespace AccessWaiterEP
{
    public class LocalRouter: IRouter,IAppInstanceFactoty
    {
        private Dictionary<int, IAppInstance> s_Instances = new Dictionary<int, IAppInstance>();
        private LocalRouter() { }
        private static LocalRouter Router = new LocalRouter();
        public static IRouter GetRouter()
        {
            return Router;
        }

        public IAppInstance FindInstance(int Instance)
        {
            return s_Instances[Instance];
        }

        public IAppInstance CreateNewInstance(out int Instance)
        {
            throw new NotImplementedException();
        }


        public int EmptyInstance
        {
            get { return 0; }
        }


        public IAppInstanceFactoty GetFactory()
        {
            return this;
        }
    }
}