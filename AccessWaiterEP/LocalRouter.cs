using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AccessWaiterEP.Infrastructure;

namespace AccessWaiterEP
{
    class LocalRouter: IRouter,IAppInstanceFactory
    {
        private Dictionary<int, IAppInstance> m_Instances = new Dictionary<int, IAppInstance>();
        Random m_IdGen = new Random();
        private LocalRouter() { }
        private static LocalRouter s_Router = new LocalRouter();

        public static IRouter GetRouter() { return s_Router; }

        public IAppInstance FindInstance(int Instance) {return m_Instances.ContainsKey(Instance)?m_Instances[Instance]:null;}
        
        public int EmptyInstance {get { return 0; } }
        
        public IAppInstanceFactory GetFactory() {return this;}

        //IAppInstanceFactoty methods
        public IAppInstance CreateNewInstance(out int Instance)
        {
            do
            {
                Instance = m_IdGen.Next(1, Int32.MaxValue);
            } while (m_Instances.ContainsKey(Instance));
            AppClass result = new AppClass(this,Instance);
            try
            {
                m_Instances.Add(Instance, result);
            }
            catch {
                result.Dispose();
                throw;
            }
            return result;
        }

        public void Release(IAppInstance AppInstance)
        {
            if (m_Instances.ContainsKey(AppInstance.Key))
            {
                m_Instances.Remove(AppInstance.Key);
                ((AppClass)AppInstance).Dispose();
            }
        }
    }
}
