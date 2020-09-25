using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManMFOperator.Tests;

namespace ManMFOperator.Infrastructure
{
    static class FakeUserRepositoryCreator
    {
        static public IUserRepository Create() { return new FakeUserRepository(); }
    }
}