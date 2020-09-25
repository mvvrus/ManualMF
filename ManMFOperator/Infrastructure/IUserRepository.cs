using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManMFOperator.Models;

namespace ManMFOperator.Infrastructure
{
    public interface IUserRepository
    {
        IEnumerable<UserInfo> GetUsers();
    }
}
