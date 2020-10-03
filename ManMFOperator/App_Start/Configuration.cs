using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ManMFOperator
{
    public static class Configuration
    {
        static Configuration()
        {
            DefaultAccessDuration = new TimeSpan(2, 0, 0);
        }
        public static void InitConfiguration() { 
            //TODO Implement
        }
        public static TimeSpan DefaultAccessDuration {get;private set;} 
    }
}