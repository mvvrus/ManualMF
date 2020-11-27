using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Data.SqlClient;
using ManualMF;



namespace AccessWaiterEP
{

    public class Global : System.Web.HttpApplication
    {
        public const String CONN_STR = "ConnStr";
        public const String FIRSTWAITTIME = "firstWaitTime";
        public const String MAXWAITTIME = "maxWaitTime";

        protected void Application_Start(object sender, EventArgs e)
        {
            //Intialize SQLDependency System
            String connStr = WebConfigurationManager.ConnectionStrings["Default"]!=null?
             WebConfigurationManager.ConnectionStrings["Default"].ConnectionString : "Server=.\\SQLExpress;Integrated Security=true;Database=ManualMF";
            String firstWaitTimeStr = WebConfigurationManager.AppSettings[FIRSTWAITTIME];
            int firstTimeWait;
            if (firstWaitTimeStr != null && Int32.TryParse(firstWaitTimeStr, out firstTimeWait) && firstTimeWait > 0) 
                Application.Add(FIRSTWAITTIME, firstTimeWait);
            else Application.Add(FIRSTWAITTIME, AccessWaiter.FIRST_WAIT_TIME);
            String maxWaitTimeStr = WebConfigurationManager.AppSettings[MAXWAITTIME];
            int maxTimeWait;
            if (maxWaitTimeStr != null && Int32.TryParse(maxWaitTimeStr, out maxTimeWait) && maxTimeWait > 0)
                Application.Add(MAXWAITTIME, maxTimeWait);
            else Application.Add(MAXWAITTIME, AccessWaiter.MAX_WAIT_TIME);
            Application.Add(CONN_STR, connStr);
            SqlDependency.Start(connStr);
        }


        protected void Application_End(object sender, EventArgs e)
        {
            //Unintialize SQLDependency System
            SqlDependency.Stop((string)Application[CONN_STR]);
        }
    }
}