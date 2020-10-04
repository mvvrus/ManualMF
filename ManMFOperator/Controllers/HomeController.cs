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
        IUserRepository m_Repository; //Repository used to work with user requests

        public HomeController(IUserRepository Repository) { m_Repository = Repository; }
        IEnumerable<UserInfo> GetUsers(Boolean IncludeNonPending)
        {
            IEnumerable<UserInfo> result = m_Repository.GetUsers();
            if (IncludeNonPending) return result;
            else return result.Where(x => AccessState.Pending == x.State);
        }

        AuthorizeAttribute m_Authorizer = null; //Cached authorizer value (rarely used but nevertheless)

        //Perform request authorization for this controller based on the group membership of the user (Windows user identity is used)
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            //Because the windows group name depends on domain name (i.e. is non-constant) of the server we cannot just use authorization attribute 
            //Authorize the user here, using the same AuthorizeAttribute class authorizer as for an attribute athorization
            //but with constructed (i.e. non-constant) Role name we use WindowsTokenRoleProvider as a role provider, so role name is a windows group name
            if (null == m_Authorizer) //no cached authorizer
            {
                m_Authorizer = new AuthorizeAttribute(); //create one
                m_Authorizer.Roles = Configuration.OperatorsGroup; //Set the authorized role - the Windows group from the configuration
            }
            m_Authorizer.OnAuthorization(filterContext); //Perform authorization (just like we use [Authorize] attribute)
        }

        //Prepare for localization in every action method
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            //Pass the Resource object used for the localization
            ViewData.Add(ResourceHtmlExtensions.LOCALIZER, Resource.GetResource(UserInfoRes.RESNAME));
        }

        //
        // GET/POST ~/Home/Index (or simply ~/) - return a list of user access requests (all or pending only)
        public ActionResult Index(Boolean? withnonpending)
        {
            //If withnonpending parameter is not set (first time call or redirection) try to get it's value from TempData
            Boolean includenonpending = withnonpending ?? (Boolean?)TempData["withnonpending"] ?? false;
            TempData["withnonpending"] = includenonpending;
            //Pass data to Index.cshtml to process
            return View(new HomeIndexViewModel() { withnonpending = includenonpending, users = GetUsers(includenonpending).OrderBy(x=>x.Fullname) });
        }

        // POST ~/Home/ClearUser - delete specified request
        [HttpPost]
        public ActionResult ClearUser(String upn)
        {
            m_Repository.ClearUser(upn); //Delete the request from repository
            return RedirectToAction("Index"); //Return to the list of access requests
        }

        //POST ~/Home/SetUserAccess - show the form to the operator to process the pending request
        [HttpPost]
        public ActionResult SetUserAccess(String upn)
        {
            UserInfo user_info = m_Repository.GetUser(upn); //Get data for the request from the repository
            if (null == user_info) throw new ArgumentException();
            return View("UserAccess", user_info); //Pass data to UserAccess.cshtml to process
        }

        //POST ~/Home/CompleteSetUserAccess - process the operator's decision about the pending request
        [HttpPost]
        public ActionResult CompleteSetUserAccess(String Upn, UserAccessAction action, Boolean sameiponly = false, int? hours=null, int? mins=null)
        {
            if (UserAccessAction.Clear == action) return ClearUser(Upn); //Process Clear command: delete the specified request via Clear action
            //Process Allow or Deny command
            //Determine the validity period for allowing/denying the request
            DateTime valid_until = DateTime.Now+Configuration.DefaultAccessDuration;
            if (hours != null && hours >= 0 && hours < 24 && mins != null && mins >= 0 && mins < 60)
            {
                valid_until = DateTime.Now.Date+new TimeSpan(hours.Value,mins.Value,0);
                if(valid_until<=DateTime.Now) valid_until=valid_until.AddDays(1);
            }
            //Allow/deny the request according to the parameters specified
            m_Repository.SetUserAccess(Upn, UserAccessAction.Allow == action, valid_until, sameiponly);
            return RedirectToAction("Index");
        }

    }
}
