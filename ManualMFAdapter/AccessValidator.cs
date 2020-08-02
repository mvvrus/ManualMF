using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data;
using System.Data.SqlClient;

namespace ManualMF
{
    internal class AccessValidator: IDisposable
    {
        //The object does not own the connection, only references it
        SqlConnection m_Connection;

        internal AccessValidator(SqlConnection Connection)
        {
            m_Connection = Connection;
            //Connection must be opened before calling Check or CheckAndRequest method
        }

        public void Dispose()
        {
            //Remove connection reference
            m_Connection = null;
        }

        //Return request state for the user

        public AccessStateAndReason Check(String Upn, IPAddress AccessedFrom)
        {
            DateTime db_valid_until=DateTime.Now;
            AccessStateAndReason result;
            result.State = AccessState.Denied;
            result.Reason = AccessDeniedReason.UnknownOrNotDenied;
            result.Token = null;
            IPAddress db_allowfromip = null;
            bool record_exists = true;
            using (SqlCommand rdcmd = new SqlCommand(
                            "SELECT VALID_UNTIL,FROM_IP,REQUEST_STATE,EPACCESSTOKEN FROM dbo.PERMISSIONS WHERE UPN=@UPN", m_Connection))
            {
                //Correct set @UPN parameter to user principal name passed
                rdcmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                SqlDataReader rdr = rdcmd.ExecuteReader();
                try
                {
                    if (rdr.HasRows)
                    {
                        rdr.Read();
                        db_valid_until = (DateTime)rdr[0];
                        if (!rdr.IsDBNull(1))
                        {
                            db_allowfromip = new IPAddress((byte[])rdr[1]);
                        }
                        if (!rdr.IsDBNull(3)) result.Token = (int)rdr[3];
                        result.State = (AccessState)rdr[2];
                    }
                    else record_exists = false;
                }
                finally
                {
                    rdr.Close();
                }
                if (!record_exists)
                {
                    result.Reason = AccessDeniedReason.RecordDisappeared;
                }
                else if (db_valid_until < DateTime.Now)
                {
                    if(AccessState.Pending==result.State) result.Reason = AccessDeniedReason.DeniedByTimeOut;
                    else result.Reason = AccessDeniedReason.RecordDisappeared;
                    result.State = AccessState.Denied;
                }
                else if (AccessState.Denied == result.State)
                {
                    result.Reason=AccessDeniedReason.DeniedByOperator;
                }
                else if ((AccessState.Allowed == result.State && null != db_allowfromip && !db_allowfromip.Equals(AccessedFrom)))
                {
                    result.State = AccessState.Denied;
                    result.Reason = AccessDeniedReason.DeniedByIP;
                }
                
                if(AccessState.Pending == result.State && db_valid_until<DateTime.Now) {
                    result.State = AccessState.Denied;
                    result.Reason = AccessDeniedReason.DeniedByTimeOut;
                }
            }
            return result;
        }

        //See if request for the user is already exists in the DB and not expired and not allowed from different IP address only
        //If so, return request state
        //Else put new request in pending state into the database
        public AccessState CheckAndRequest(String Upn, IPAddress AccessedFrom, DateTime ValidUntil, out int? Token)
        {
            //Check end of validity period passed
            if(ValidUntil == null || ValidUntil<DateTime.Now) {
                throw new ArgumentException("Argument \"ValidUntil\" is null or points to a moment in the past.");
            }
            AccessState result = AccessState.Pending;
            using (SqlTransaction tran = m_Connection.BeginTransaction())
            {
                try //Perform all operations in the transaction
                {
                    //Search for the DB record with this Upn value
                    DateTime db_valid_until = ValidUntil;
                    AccessState db_state = AccessState.Pending;
                    IPAddress db_allowfromip = null;
                    Boolean db_record_exists;
                    Token = null;
                    using (SqlCommand rdcmd = new SqlCommand(
                        "SELECT VALID_UNTIL,FROM_IP,REQUEST_STATE,EPACCESSTOKEN FROM dbo.PERMISSIONS WHERE UPN=@UPN", 
                        m_Connection,
                        tran))
                    {
                        //Correct set @UPN parameter to user principal name passed
                        rdcmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                        SqlDataReader rdr = rdcmd.ExecuteReader();
                        try
                        {
                            db_record_exists = rdr.HasRows;
                            if (db_record_exists)
                            {
                                rdr.Read();
                                db_valid_until = (DateTime)rdr[0];
                                if (!rdr.IsDBNull(1)) db_allowfromip = new IPAddress((byte[])rdr[1]);
                                db_state = (AccessState)rdr[2];
                                if (!rdr.IsDBNull(3)) Token = (int)rdr[3];
                            }
                        }
                        finally
                        {
                            rdr.Close();
                        }
                    }
                    using (SqlCommand insupdcmd = new SqlCommand("", m_Connection, tran))
                    {
                        //If no records found - add DB record with pending state and ValidUntil as validity period and return that state
                        if (!db_record_exists)
                        {
                            insupdcmd.CommandText = "INSERT INTO dbo.PERMISSIONS(UPN,VALID_UNTIL,REQUEST_STATE,FROM_IP,EPACCESSTOKEN) VALUES(@UPN,@VALID_UNTIL,@REQUEST_STATE,@FROM_IP,@EPACCESSTOKEN)";
                            //Initilaize endpoint access token value
                            Token=new Random().Next(Int32.MaxValue);
                            insupdcmd.Parameters.Add("@EPACCESSTOKEN", SqlDbType.NVarChar).Value = (int?)Token;
                        }
                        //else it is within validity period and condition "allowed, but not from this ip" is not met - return the state from DB record
                        else if (db_valid_until >= DateTime.Now &&
                            (db_state != AccessState.Allowed || null == db_allowfromip || db_allowfromip.Equals(AccessedFrom)))
                        {
                            result = db_state;
                            //Leave insupdcmd command text blank to indicate that no execution is needed
                        }
                        //else update the existing record to reflect its new state (pending) and validity period (from ValidUntil param)
                        else
                        {
                            insupdcmd.CommandText = "UPDATE dbo.PERMISSIONS SET VALID_UNTIL=@VALID_UNTIL, REQUEST_STATE=@REQUEST_STATE, FROM_IP=@FROM_IP  WHERE UPN=@UPN";
                        }
                        if (insupdcmd.CommandText!="")
                        {//Execute the update/insert command if required
                            //Set parameters
                            insupdcmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                            insupdcmd.Parameters.Add("@VALID_UNTIL", SqlDbType.DateTime).Value = ValidUntil;
                            insupdcmd.Parameters.Add("@REQUEST_STATE", SqlDbType.TinyInt).Value = AccessState.Pending;
                            SqlParameter p_from_ip = insupdcmd.Parameters.Add("@FROM_IP", SqlDbType.Binary);
                            p_from_ip.IsNullable = true;
                            if (AccessedFrom != null)
                            {
                                byte[] from_ip_bytes = AccessedFrom.GetAddressBytes();
                                p_from_ip.Size = from_ip_bytes.Length;
                                p_from_ip.Value = from_ip_bytes;
                            }
                            else p_from_ip.Value = DBNull.Value;
                            insupdcmd.ExecuteNonQuery();
                        }
                    }
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    //Try to rollback the transaction
                    try
                    {
                        tran.Rollback();
                    }
                    catch { } //Just "eat" any exceptions during rollback (usually it means that connection is lost)
                    throw ex;
                }
            }
            return result;
        }

        //Cancel pending request from the same IP. Return true if successful, false otherwise
        public Boolean Cancel(String Upn, IPAddress AccessedFrom)
        {
            Boolean result;
            using (SqlCommand cmd = new SqlCommand("DELETE FROM dbo.PERMISSIONS WHERE UPN=@UPN AND REQUEST_STATE=0 AND FROM_IP=@FROM_IP",m_Connection) ){
                cmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                SqlParameter p_from_ip = cmd.Parameters.Add("@FROM_IP", SqlDbType.Binary);
                p_from_ip.IsNullable = true;
                if (AccessedFrom != null)
                {
                    byte[] from_ip_bytes = AccessedFrom.GetAddressBytes();
                    p_from_ip.Size = from_ip_bytes.Length;
                    p_from_ip.Value = from_ip_bytes;
                }
                else p_from_ip.Value = DBNull.Value;
                result=cmd.ExecuteNonQuery()>0;
            }
            return result;
        }
    }
}
