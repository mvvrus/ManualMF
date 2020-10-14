using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ManMFOperator.Models;
using ManualMF;

namespace ManMFOperator.Infrastructure
{
    public class UserRepository:IUserRepository
    {

        IDirectoryAccessor m_DirectoryAccessor;
        IDBAccessor m_DBAccessor;
        public UserRepository(IDirectoryAccessor DirectoryAccessor, IDBAccessor DBAccessor)
        {
            m_DirectoryAccessor = DirectoryAccessor;
            m_DBAccessor = DBAccessor;
        }

        UserInfo FillFromDirectory(DBUserInfo raw_info)
        {
            UserInfo info = new UserInfo() { Upn = raw_info.Upn, State = raw_info.State, IPAddress = raw_info.IPAddress, ValidUntil = raw_info.ValidUntil };
            DirectoryUserInfo ad_info = m_DirectoryAccessor.GetADUserInfo(info.Upn);
            if (ad_info != null)
            {
                info.Fullname = ad_info.FullName;
                info.Department = ad_info.Department;
            }
            return info;
        }
        public IEnumerable<UserInfo> GetUsers()
        {
            IEnumerable<DBUserInfo> db_data = m_DBAccessor.GetUsers();

            List<UserInfo> result = new List<UserInfo>();
            foreach (var raw_info in db_data)
            {
                UserInfo info = FillFromDirectory(raw_info);
                result.Add(info);
            }
            return result;
        }

        public void ClearUser(string Upn)
        {
            m_DBAccessor.ClearUser(Upn);
        }

        public UserInfo GetUser(string Upn)
        {
            DBUserInfo db_data = m_DBAccessor.GetUsers().Where(x=>x.Upn==Upn).FirstOrDefault();
            return db_data != null ? FillFromDirectory(db_data) : null;
        }

        public void SetUserAccess(string Upn, bool Allow, DateTime ValidUntil, bool ThisIPOnly = false)
        {
            m_DBAccessor.SetUserAccess(Upn, Allow, ValidUntil, ThisIPOnly);
        }
    }

    static class UserRepositoryCreator
    {
        static IUserRepository m_Repository = null;
        static public IUserRepository Create()
        {
            IDirectoryAccessor dir_access = (IDirectoryAccessor)DependencyResolver.Current.GetService(typeof(IDirectoryAccessor));
            IDBAccessor db_access = (IDBAccessor)DependencyResolver.Current.GetService(typeof(IDBAccessor));
            if (null == m_Repository) m_Repository = new UserRepository(dir_access,db_access);
            return m_Repository;
        }
    }

}