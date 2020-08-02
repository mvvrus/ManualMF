using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessWaiterEP.Infrastructure
{
    
    public interface IRouter
    {
        int EmptyInstance{get;}
        IAppInstance FindInstance(int Instance);
        IAppInstanceFactory GetFactory();
    }

    public interface IAppInstanceFactory
    {
        IAppInstance CreateNewInstance(out int Instance);
        void Release(IAppInstance AppInstance);
    }
}
