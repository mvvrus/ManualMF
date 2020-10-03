using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManualMF;
using System.ComponentModel.DataAnnotations;
using ManMFOperator;
using ManMFOperator.Infrastructure;

namespace ManMFOperator.Models
{
    //Model class containing data for a user with the specified access
    public class UserInfo
    {
        [Display(Name = "Fullname", ResourceType=typeof(UserInfoRes))]
        public String Fullname { get; set; }
        [Display(Name = "Department", ResourceType = typeof(UserInfoRes))]
        public String Department { get; set; }
        [Display(Name = "Upn", ResourceType = typeof(UserInfoRes))]
        public String Upn { get; set; }
        [Display(Name = "State", ResourceType = typeof(UserInfoRes))]
        public AccessState State { get; set; }
        [Display(Name = "ValidUntil", ResourceType = typeof(UserInfoRes))]
        public DateTime ValidUntil { get; set; }
        [Display(Name = "IPAddress", ResourceType = typeof(UserInfoRes))]
        public String IPAddress { get; set; }
    }
}

//A class to be used to localize UserInfo displayed field names via DisplayAttribute
public static class UserInfoRes
{
    public const String RESNAME = "UserInfoRes"; //Name of embedded resources file to be used in localization 
    static Resource s_Resource; //Resource object to be used in localization
    static UserInfoRes() { s_Resource = Resource.GetResource(RESNAME); }
    public static String Fullname { get { return null == s_Resource ? "Fullname" : s_Resource.GetResourceString("Fullname"); } }
    public static String Department { get { return null == s_Resource ? "Department" : s_Resource.GetResourceString("Department"); } }
    public static String Upn { get { return null == s_Resource ? "Upn": s_Resource.GetResourceString("Upn"); } }
    public static String State { get { return null == s_Resource ? "State" : s_Resource.GetResourceString("State"); } }
    public static String ValidUntil { get { return null == s_Resource ? "ValidUntil" : s_Resource.GetResourceString("ValidUntil"); } }
    public static String IPAddress { get { return null == s_Resource ? "IPAddress" : s_Resource.GetResourceString("IPAddress"); } }
}
