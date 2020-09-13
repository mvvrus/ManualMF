using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Configuration;
using System.Data.SqlClient;



namespace AccessWaiterEP
{

    public class Global : System.Web.HttpApplication
    {
        const String CONN_STR = "ConnStr";

        protected void Application_Start(object sender, EventArgs e)
        {
            //Intialize SQLDependency System
            String connStr = WebConfigurationManager.ConnectionStrings["Default"].ConnectionString;
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