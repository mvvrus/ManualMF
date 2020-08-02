using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessWaiterEP.Infrastructure
{
    public interface IAppInstance
    {
        Task<String> DispatchAsync(String Json);
        void Abandon(Task OutstandingTask);
        int Key { get; }
        bool CanRelease();
    }
}
