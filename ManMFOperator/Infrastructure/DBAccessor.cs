using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Configuration;
using System.Data.SqlClient;
using ManualMF;
using ManMFOperator.Models;
using System.Net;

namespace ManMFOperator.Infrastructure
{
    public class DBAccessor:IDBAccessor
    {
        SqlConnection m_Connection;
        AccessController m_Controller;

        public DBAccessor()
        {
            m_Connection = new SqlConnection(Configuration.ConnString);
            m_Controller = new AccessController(m_Connection);
        }

        private void FillInfo(SqlDataReader rdr,DBUserInfo info)
        {
            info.Upn = rdr.GetString(0);
            info.State = (AccessState)(rdr.GetByte(1));
            info.ValidUntil = rdr.GetDateTime(2);
            if (rdr.IsDBNull(3)) info.IPAddress = null;
            else info.IPAddress = (new IPAddress((byte[])rdr[3])).ToString();
        }
        public IEnumerable<DBUserInfo> GetUsers()
        {
            List<DBUserInfo> result = new List<DBUserInfo>();
            m_Connection.Open();
            try
            {
                String query_str = "SELECT UPN, REQUEST_STATE, VALID_UNTIL, FROM_IP FROM dbo.PERMISSIONS WHERE VALID_UNTIL>SYSDATETIME()";
                using (SqlCommand cmd = new SqlCommand(query_str, m_Connection)) {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            DBUserInfo info = new DBUserInfo();
                            FillInfo(rdr, info);
                            result.Add(info);
                        }
                    }
                }
            }
            finally
            {
                m_Connection.Close();
            }
            return result;
        }

        public void ClearUser(string Upn)
        {
            m_Connection.Open();
            try
            {
                m_Controller.Clear(Upn);
            }
            finally
            {
                m_Connection.Close();
            }
        }

        public DBUserInfo GetUser(string Upn)
        {
            DBUserInfo info = null;
            m_Connection.Open();
            try
            {
                String query_str = "SELECT UPN, REQUEST_STATE, VALID_UNTIL, FROM_IP FROM dbo.PERMISSIONS WHERE VALID_UNTIL>SYSDATETIME() AND UPN=@UPN";
                using (SqlCommand cmd = new SqlCommand(query_str, m_Connection))
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            rdr.Read();
                            info = new DBUserInfo();
                            FillInfo(rdr, info);
                        }
                    }
                }
            }
            finally
            {
                m_Connection.Close();
            }
            return info;
        }

        public void SetUserAccess(string Upn, bool Allow, DateTime ValidUntil, bool ThisIPOnly = false)
        {
            m_Connection.Open();
            try
            {
                if (Allow) m_Controller.Allow(Upn, ValidUntil, ThisIPOnly);
                else m_Controller.Deny(Upn, ValidUntil);
            }
            finally
            {
                m_Connection.Close();
            }
        }
    }
}