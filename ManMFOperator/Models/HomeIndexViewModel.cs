using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManMFOperator.Infrastructure;

namespace ManMFOperator.Models
{
    public class HomeIndexViewModel
    {
        public Boolean withnonpending { get; set; }
        public IEnumerable<UserInfo> users { get; set; }
    }
}