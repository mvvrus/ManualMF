using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManMFOperator.Tests;

namespace ManMFOperator.Infrastructure
{
    static class FakeUserRepositoryCreator
    {
        static IUserRepository m_Repository = null;
        static public IUserRepository Create() { 
            if(null==m_Repository) m_Repository=new FakeUserRepository();
            return m_Repository;
        }
    }
}