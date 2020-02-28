using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualMF
{
    //SQL Database connection manager
    //Encapsulates database connection management strategy
    //Each connection manager object is designed to be a part of authentication context
    //whereas the connection object it presents may be independent of the context.
    //Possible stragies for database connection management are:
    // opened on demand, created once per context and kept in the object or be a part of the connection pool
    //
    //Currently implemented strategy uses the third approach - using native SQL server client connection pooling
    class ConnManager: IDisposable
    {
        SqlConnection m_Connection; //The database connection object refernce. ConnManager owns thr connection

        internal static ConnManager GetNewManager() { return new ConnManager(); }
        internal SqlConnection Acquire() {
            m_Connection.Open();
            return m_Connection;
        }
        internal void Release() { m_Connection.Close(); } 
        private ConnManager()
        {
            m_Connection = new SqlConnection(Configuration.DBConnString + ";Min Pool Size=1");
        }

        public void Dispose()
        {
            if (m_Connection != null)
            {
                try { m_Connection.Dispose(); } catch { }
                m_Connection = null;
            }
        }
    }
}
