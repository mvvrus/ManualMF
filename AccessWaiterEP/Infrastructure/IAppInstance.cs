using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessWaiterEP.Infrastructure
{
    public interface IAppInstance
    {
        Task<String> CallAsyncMethodAsync(String Json);
        Boolean TryRelease();
        void ReleaseReference(Task<String> OutstandingTask);
    }
}
