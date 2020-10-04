using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace ManMFOperator
{
    public static class Configuration
    {
        static string s_DomainShortName; //Short name of the domain containing this server
        static string s_OperatorsGroup = "ManualMF_Operators";

        static Configuration()
        {
            //Get short name for the server's domain
            // 1.Get the domain's DN
            String domain_dn = (String)Domain.GetComputerDomain().GetDirectoryEntry().Properties["distinguishedName"].Value;
            // 2.Get the DN of the configuration container
            String config_dn = (String) new DirectoryEntry("LDAP://RootDSE").Properties["configurationNamingContext"].Value;
            // 3.Bind to the configuration container root
            DirectoryEntry config_partition = new DirectoryEntry("LDAP://" + config_dn);
            // 4.Searh for the crossRef object for the compurer domain and read it's short (or NetBIOS) name
            DirectorySearcher cref_search = new DirectorySearcher(config_partition,"(nCName="+domain_dn+")",new String[]{"nETBIOSName"});
            SearchResult cref = cref_search.FindOne();
            s_DomainShortName = (String)cref.Properties["nETBIOSName"][0];
            //
            DefaultAccessDuration = new TimeSpan(2, 0, 0);
        }
        public static void InitConfiguration() { 
            //TODO Implement reading the configuration from web.config
        }
        public static TimeSpan DefaultAccessDuration {get;private set;}
        public static String OperatorsGroup { get { return s_DomainShortName+"\\"+s_OperatorsGroup; } }
    }
}
