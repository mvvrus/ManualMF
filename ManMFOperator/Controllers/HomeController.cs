using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ManMFOperator.Infrastructure;
using ManMFOperator.Models;
using ManualMF;

namespace ManMFOperator.Controllers
{
    public class HomeController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewData.Add(ResourceHtmlExtensions.LOCALIZER, Resource.GetResource(UserInfoRes.RESNAME));
        }
        //
        // GET: /Home/
        IUserRepository m_Repository;
        public HomeController(IUserRepository Repository) { m_Repository = Repository; }
        IEnumerable<UserInfo> GetUsers(Boolean IncludeNonPending)
        {
            IEnumerable<UserInfo> result = m_Repository.GetUsers();
            if (IncludeNonPending) return result;
            else return result.Where(x => AccessState.Pending == x.State);
        }

        public ActionResult Index(Boolean? withnonpending)
        {
            Boolean includenonpending = withnonpending ?? (Boolean?)TempData["withnonpending"] ?? false;
            TempData["withnonpending"] = includenonpending;
            return View(new HomeIndexViewModel() { withnonpending = includenonpending, users = GetUsers(includenonpending).OrderBy(x=>x.Fullname) });
        }

        public ActionResult ClearUser(String upn)
        {
            m_Repository.ClearUser(upn);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SetUserAccess(String upn)
        {
            UserInfo user_info = m_Repository.GetUser(upn);
            if (null == user_info) throw new ArgumentException();
            return View("UserAccess", user_info);
        }

        [HttpPost]
        public ActionResult CompleteSetUserAccess(String Upn, UserAccessAction action, Boolean sameiponly = false, int? hours=null, int? mins=null)
        {
            if (UserAccessAction.Clear == action) m_Repository.ClearUser(Upn);
            else {
                DateTime valid_until = DateTime.Now+Configuration.DefaultAccessDuration;
                if (hours != null && hours >= 0 && hours < 24 && mins != null && mins >= 0 && mins < 60)
                {
                    valid_until = DateTime.Now.Date+new TimeSpan(hours.Value,mins.Value,0);
                    if(valid_until<=DateTime.Now) valid_until=valid_until.AddDays(1);
                }
                m_Repository.SetUserAccess(Upn, UserAccessAction.Allow == action, valid_until, sameiponly);
            }
            return RedirectToAction("Index");
        }

    }
}
