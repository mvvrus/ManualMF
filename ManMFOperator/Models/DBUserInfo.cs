using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManualMF;

namespace ManMFOperator.Models
{
    public class DBUserInfo
    {
        public String Upn { get; set; }
        public AccessState State { get; set; }
        public DateTime ValidUntil { get; set; }
        public String IPAddress { get; set; }

    }
}