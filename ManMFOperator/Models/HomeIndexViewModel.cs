using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using ManMFOperator.Infrastructure;

namespace ManMFOperator.Models
{
    public class HomeIndexViewModel
    {
        [Display(Name = "withnonpending", ResourceType = typeof(HomeIndexViewModelRes))]
        public Boolean withnonpending { get; set; }
        public IEnumerable<UserInfo> users { get; set; }

    }

    public class HomeIndexViewModelRes
    {
        static Resource s_Resource;
        static HomeIndexViewModelRes() { s_Resource = Resource.GetResource(UserInfoRes.RESNAME); }
        public static String withnonpending { get { return s_Resource.GetResourceString("withnonpending"); } }
    }
}

