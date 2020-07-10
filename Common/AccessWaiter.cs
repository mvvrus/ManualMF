using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security;


namespace ManualMF
{
    [Serializable]
    public sealed class QueryNotificationException : Exception, ISerializable
    {
        [NonSerialized]
        SqlNotificationEventArgs m_event_args;

        private static String MakeDefaultMessage(SqlNotificationEventArgs EventArgs)
        {
            if (EventArgs != null) return String.Format("(Type: {0}, Info: {1}, Source: {2})",EventArgs.Type,EventArgs.Info,EventArgs.Source);
            else return "(no additional data)";
        }

        public QueryNotificationException() :base()
        {
        }

        public QueryNotificationException(SqlNotificationEventArgs EventArgs):base(MakeDefaultMessage(EventArgs))
        {
            m_event_args = EventArgs;
        }

        public QueryNotificationException(String Message, SqlNotificationEventArgs EventArgs)
            : base(Message + MakeDefaultMessage(EventArgs))
        {
            m_event_args = EventArgs;
        }

        public QueryNotificationException(String Message, Exception InnerException, SqlNotificationEventArgs EventArgs)
            : base(Message+MakeDefaultMessage(EventArgs), InnerException)
        {
            m_event_args = EventArgs;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        private QueryNotificationException(SerializationInfo info, StreamingContext context):base(info, context)
        {
            m_event_args = null;
            Boolean _has_args = info.GetBoolean("QNXHasArgs");
            if(_has_args) {
                SqlNotificationInfo _info = (SqlNotificationInfo)info.GetValue("QNXInfo", typeof(SqlNotificationInfo));
                SqlNotificationSource _source = (SqlNotificationSource)info.GetValue("QNXSource", typeof(SqlNotificationSource));
                SqlNotificationType _type = (SqlNotificationType)info.GetValue("QNXType", typeof(SqlNotificationType));
                m_event_args = new SqlNotificationEventArgs(_type, _info, _source);
            }
            throw new NotImplementedException();
        }

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info,context);
            Boolean has_args = m_event_args != null;
            info.AddValue("QNXHasArgs", has_args);
            if (has_args)
            {
                info.AddValue("QNXInfo", m_event_args.Info);
                info.AddValue("QNXSource", m_event_args.Source);
                info.AddValue("QNXType", m_event_args.Type);
            }
        }
    }

    internal class AccessWaiter
    {
        private enum EndWaitReason { NoEndWait = -1, Changed = 0, Forbidden = 1};

        private SqlConnection m_Connection;
        private AutoResetEvent m_Event = new AutoResetEvent(false);
        private SqlNotificationEventArgs m_ChangeArgs = null;

        public AccessWaiter(SqlConnection Connection)  { m_Connection = Connection; }
        public Boolean WaitForAccessChange(String Upn, String EPAccessToken)
        {
            SqlCommand rdcmd = new SqlCommand("SELECT EPACCESSTOKEN,VALID_UNTIL,REQUEST_STATE FROM dbo.PERMISSIONS WHERE UPN=@UPN AND VALID_UNTIL>@VALID_UNTIL", m_Connection);
            rdcmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
            SqlParameter valid_until_param=rdcmd.Parameters.Add("@VALID_UNTIL", SqlDbType.DateTime);
            Boolean access_checked = null == EPAccessToken;
            EndWaitReason ewr = EndWaitReason.NoEndWait;
            DateTime valid_until = DateTime.MinValue;
            Boolean first_run = true;
            do
            {
                TimeSpan timeout;
                DateTime this_moment = DateTime.Now;
                rdcmd.Notification = null;
                valid_until_param.Value = this_moment;
                timeout = first_run ? new TimeSpan(0, 0, 10) : valid_until - this_moment;
                SqlDependency dep = new SqlDependency(rdcmd, null, (int)Math.Ceiling(timeout.TotalSeconds > 1 ? timeout.TotalSeconds : 1));
                dep.OnChange += (sender, e) => { m_ChangeArgs = e; m_Event.Set(); };

                SqlDataReader rdr = rdcmd.ExecuteReader();
                try
                {
                    if (rdr.HasRows)
                    {
                        rdr.Read();
                        access_checked = access_checked || (rdr.IsDBNull(0) || ((String)rdr[0]).Equals(EPAccessToken));
                        valid_until = (DateTime)rdr[1];
                        if (!access_checked) ewr = EndWaitReason.Forbidden;
                        else if (first_run && (AccessState)rdr[2] != AccessState.Pending) ewr = EndWaitReason.Changed;
                    }
                    else ewr = access_checked?EndWaitReason.Changed:EndWaitReason.Forbidden;
                }
                finally
                {
                    rdr.Close();
                }
                if (first_run)
                {
                    rdcmd = new SqlCommand("SELECT EPACCESSTOKEN,VALID_UNTIL FROM dbo.PERMISSIONS WHERE UPN=@UPN AND VALID_UNTIL>@VALID_UNTIL AND REQUEST_STATE=0", m_Connection);
                    rdcmd.Parameters.Add("@UPN", SqlDbType.NVarChar).Value = Upn;
                    valid_until_param = rdcmd.Parameters.Add("@VALID_UNTIL", SqlDbType.DateTime);
                    first_run = false;
                }
                if (EndWaitReason.NoEndWait == ewr)
                {
                    m_Event.WaitOne();
                    if ( m_ChangeArgs.Type != SqlNotificationType.Change) throw new QueryNotificationException(m_ChangeArgs);
                }
            } while (EndWaitReason.NoEndWait==ewr);
            return EndWaitReason.Changed==ewr;
        }
    }
}
