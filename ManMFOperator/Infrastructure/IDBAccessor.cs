using System;
using System.Collections.Generic;
using ManMFOperator.Models;

namespace ManMFOperator.Infrastructure
{
    public interface IDBAccessor
    {
        IEnumerable<DBUserInfo> GetUsers();
        void ClearUser(String Upn);
        DBUserInfo GetUser(String Upn);
        void SetUserAccess(String Upn, Boolean Allow, DateTime ValidUntil, bool ThisIPOnly = false);

    }
}
