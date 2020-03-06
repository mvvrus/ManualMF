using System;
using System.Net;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Runtime.Serialization;


namespace ManualMF
{
    [Serializable]
    class ToManyRecodsAffectedException : Exception {
        public ToManyRecodsAffectedException() : base("More than one records affected. Erroneous SQL or bad database contents."){}
        protected ToManyRecodsAffectedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    class AccessController : IDisposable
    {
        //The object does not own the connection, only references it
        SqlConnection m_Connection;

        internal AccessController(SqlConnection Connection)
        {
            m_Connection = Connection;
            //Connection must be opened before calling Allow and Deny methods
        }

        public void Dispose()
        {
            //Remove connection reference
            m_Connection = null;
        }

        Boolean UpdateUpnRecord(String Upn, DateTime ValidUntil, Boolean FromRequestIPOnly, AccessState State)
        {
            int rc;
            StringBuilder sqlstr = new StringBuilder("UPDATE dbo.PERMISSIONS SET VALID_UNTIL=@VALID_UNTIL, REQUEST_STATE=@REQUEST_STATE",256);
            if (!FromRequestIPOnly) sqlstr.Append(", FROM_IP=@FROM_IP");
            sqlstr.Append(" WHERE UPN=@UPN AND REQUEST_STATE=0 AND VALID_UNTIL>SYSDATETIME()");
            using (SqlCommand cmd = new SqlCommand(sqlstr.ToString(), m_Connection) )
            {
                cmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                cmd.Parameters.Add("@VALID_UNTIL", SqlDbType.DateTime).Value = ValidUntil;
                cmd.Parameters.Add("@REQUEST_STATE", SqlDbType.TinyInt).Value = State;
                if (!FromRequestIPOnly) 
                {
                    SqlParameter p_from_ip = cmd.Parameters.Add("@FROM_IP", SqlDbType.Binary);
                    p_from_ip.IsNullable = true;
                    p_from_ip.Value = DBNull.Value;
                }
                rc=cmd.ExecuteNonQuery();
            }
            if (rc > 1) throw new ToManyRecodsAffectedException();
            return (1 == rc);
        }

        public Boolean Allow(String Upn, DateTime ValidUntil, Boolean FromRequestIPOnly)
        {
            return UpdateUpnRecord(Upn, ValidUntil, FromRequestIPOnly, AccessState.Allowed);
        }

        public Boolean Deny(String Upn, DateTime ValidUntil)
        {
            return UpdateUpnRecord(Upn, ValidUntil, false, AccessState.Denied);
        }

        public Boolean Clear(String Upn)
        {
            int rc;
            using (SqlCommand cmd = new SqlCommand("DELETE FROM dbo.PERMISSIONS WHERE UPN=@UPN", m_Connection))
            {
                cmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                rc = cmd.ExecuteNonQuery();
            }
            if (rc > 1) throw new ToManyRecodsAffectedException();
            return (1 == rc);
        }
        
    }
}
