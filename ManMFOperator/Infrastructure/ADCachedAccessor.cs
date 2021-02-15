using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using ManMFOperator.Models;


namespace ManMFOperator.Infrastructure
{
    public class ADCachedAccessor:IDirectoryAccessor
    {
        Dictionary<String, DirectoryUserInfo> m_Cache;
        DirectoryEntry m_GC;
        public ADCachedAccessor()
        {
            m_Cache = new Dictionary<String, DirectoryUserInfo>();
            m_GC = new DirectoryEntry("GC://"+Forest.GetCurrentForest().Name);
        }

        static String GetProp(SearchResult result,String prop) {
            ResultPropertyValueCollection prop_value = result.Properties[prop];
            if(prop_value.Count>0) return (string)prop_value[0];
            else return "";
        }

        public DirectoryUserInfo GetADUserInfo(String Upn)
        {
            DirectoryUserInfo info = null;
            if(!m_Cache.ContainsKey(Upn)) {
                DirectorySearcher dirsearch = new DirectorySearcher(m_GC, "(userPrincipalName=" + Upn + ")",new String[]{"department","displayName"});
                SearchResult result=dirsearch.FindOne();
                if (result != null)
                {
                    info = new DirectoryUserInfo() { Upn = Upn, FullName = GetProp(result, "displayName"), Department = GetProp(result, "department") };
                    m_Cache.Add(Upn, info);
                }
                else
                    info = new DirectoryUserInfo() { Upn = Upn, FullName = "???", Department = "???" };
            }
            return info ?? m_Cache[Upn];
        }
    }

    static public class ADCachedAccessorCreator
    {
        static ADCachedAccessor s_Accessor = null;
        static public IDirectoryAccessor Create()
        {
            if (null == s_Accessor) s_Accessor = new ADCachedAccessor();
            return s_Accessor;
        }

    }
}