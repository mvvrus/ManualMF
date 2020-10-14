using System;
using ManMFOperator.Models;

namespace ManMFOperator.Infrastructure
{
    public interface IDirectoryAccessor
    {
        DirectoryUserInfo GetADUserInfo(String Upn);
    }
}
