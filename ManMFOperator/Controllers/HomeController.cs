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

        public ActionResult Index(Boolean withnonpending=true)
        {
            return View(new HomeIndexViewModel() { withnonpending = withnonpending, users = GetUsers(withnonpending) });
        }

    }
}
