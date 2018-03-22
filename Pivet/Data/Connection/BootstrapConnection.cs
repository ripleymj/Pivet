using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace Pivet.Data.Connection
{
    internal class BootstrapConnection : IConnectionProvider
    {
        ConnectionConfig connParams;

        public Tuple<OracleConnection, bool, string> GetConnection()
        {
            if (connParams == null)
            {
                return new Tuple<OracleConnection, bool, string>(null, false, "Parameters not set.");
            }

            if (connParams.TNS_ADMIN.Length > 0)
            {
                Environment.SetEnvironmentVariable("TNS_ADMIN", connParams.TNS_ADMIN);
            }

            OracleConnectionStringBuilder csb = new OracleConnectionStringBuilder();
            csb.DataSource = $"{connParams.TNS}";
            csb.UserID = $"{connParams.BootstrapParameters.User}";
            csb.Password = $"{connParams.BootstrapParameters.Password}";
            csb.ConnectionTimeout = 120;
            csb.MaxPoolSize = 20;
            csb.MinPoolSize = 20;
            csb.ConnectionTimeout = 60;
            OracleConnection conn = new OracleConnection(csb.ConnectionString);
            conn.ConnectionOpen += ConOpenCallback;

            try
            {
            //    conn.Open();
                return new Tuple<OracleConnection, bool, string>(conn, true, "");
            }
            catch (Exception ex)
            {
                return new Tuple<OracleConnection, bool, string>(null, false, "Failed to get oracle connection: " + ex.Message);
            }
        }

        public void SetParameters(ConnectionConfig parms)
        {
            connParams = parms;
        }

        public void ConOpenCallback(OracleConnectionOpenEventArgs eventArgs)
        {
            Console.WriteLine("opening new connection");
            if (connParams.Schema != null && connParams.Schema.Length > 0)
            {
                OracleCommand cmd = new OracleCommand($"ALTER SESSION SET CURRENT_SCHEMA={connParams.Schema}", eventArgs.Connection);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }
    }
}
