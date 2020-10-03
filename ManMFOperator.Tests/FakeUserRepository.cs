using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManMFOperator.Infrastructure;
using ManMFOperator.Models;
using ManualMF;

namespace ManMFOperator.Tests
{
    class FakeUserRepository: IUserRepository
    {
        internal List<UserInfo> m_Users;
        internal FakeUserRepository()
        {
            m_Users = new List<UserInfo>();
            m_Users.Add(new UserInfo() { Fullname = "Иванов Сергей Викторович", Department = "Отдел продаж", Upn = "ivanovsv@domain.loc", State = AccessState.Pending, ValidUntil = DateTime.Now.AddMinutes(15), IPAddress = "8.8.8.8" });
            m_Users.Add(new UserInfo() { Fullname = "Алехина Анна Ивановна", Department = "Бухгалтерия", Upn = "alehinaai@domain.loc", State = AccessState.Allowed, ValidUntil = DateTime.Now.AddMinutes(240),IPAddress="8.8.8.8"});
            m_Users.Add(new UserInfo() { Fullname = "Суриков Игорь Петрович", Department = "Отдел продаж", Upn = "surikovip@domain.loc", State = AccessState.Denied, ValidUntil = DateTime.Now.AddMinutes(120),IPAddress=null});
            m_Users.Add(new UserInfo() { Fullname = "Родин Андрей Викторович", Department = "Отдел сервиса", Upn = "rodinav@domain.loc", State = AccessState.Pending, ValidUntil = DateTime.Now.AddMinutes(10),IPAddress="8.8.4.4"});
            m_Users.Add(new UserInfo() { Fullname = "Селезнев Василий Петрович", Department = "Отдел маркенинга", Upn = "seleznevvp@domain.loc", State = AccessState.Allowed, ValidUntil = DateTime.Now.AddMinutes(97), IPAddress = null });
        }
        public IEnumerable<UserInfo> GetUsers()
        {
            return m_Users;
        }


        public void ClearUser(string Upn)
        {
            m_Users.Remove(m_Users.Find(x => x.Upn == Upn));
        }


        public UserInfo GetUser(string Upn)
        {
            return m_Users.Find(x => x.Upn == Upn);
        }


        public void SetUserAccess(string Upn, bool Allow, DateTime ValidUntil, bool ThisIPOnly=false)
        {
            UserInfo user_info = GetUser(Upn);
            if (user_info != null)
            {
                user_info.State = Allow ? AccessState.Allowed : AccessState.Denied;
                user_info.ValidUntil = ValidUntil;
                if (!ThisIPOnly) user_info.IPAddress = null;
            }
            else throw new ArgumentException();
        }
    }
}
