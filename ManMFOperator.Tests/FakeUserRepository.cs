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
            m_Users.Add(new UserInfo() { Fullname = "Иванов Сергей Викторович", Department = "Отдел продаж", Upn = "ivanovsv@domain.loc", State = AccessState.Pending, ValidUntil = DateTime.Now.AddMinutes(15),IPAddress=null});
            m_Users.Add(new UserInfo() { Fullname = "Алехина Анна Ивановна", Department = "Бухгалтерия", Upn = "alehinaai@domain.loc", State = AccessState.Allowed, ValidUntil = DateTime.Now.AddMinutes(240),IPAddress="8.8.8.8"});
            m_Users.Add(new UserInfo() { Fullname = "Суриков Игорь Петрович", Department = "Отдел продаж", Upn = "surikovip@domain.loc", State = AccessState.Denied, ValidUntil = DateTime.Now.AddMinutes(120),IPAddress=null});
            m_Users.Add(new UserInfo() { Fullname = "Родин Андрей Викторович", Department = "Отдел сервиса", Upn = "rodinav@domain.loc", State = AccessState.Pending, ValidUntil = DateTime.Now.AddMinutes(10),IPAddress="8.8.4.4"});
            m_Users.Add(new UserInfo() { Fullname = "Селезнев Василий Петрович", Department = "Отдел маркенинга", Upn = "seleznevvp@domain.loc", State = AccessState.Allowed, ValidUntil = DateTime.Now.AddMinutes(97), IPAddress = null });
        }
        public IEnumerable<UserInfo> GetUsers()
        {
            return m_Users;
        }
    }
}
