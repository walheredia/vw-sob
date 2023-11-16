using System.Data.Odbc;

namespace vwsob
{
    public class Database
    {
        private string connection;

        public Database(string connectionString)
        {
            connection = connectionString;
        }

        public OdbcConnection OpenConnection()
        {
            OdbcConnection newconnection = new OdbcConnection(connection);
            newconnection.Open();
            return newconnection;
        }

        public void CloseConnection(OdbcConnection connection)
        {
            connection.Close();
        }
    }
}
