using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace ClipBoard.Repository
{
    public class DatabaseConnection
    {
        SQLiteConnection m_dbConnection;
        private readonly string databaseName = "clipboard.sqlite";

        public DatabaseConnection()
        {
            if (!File.Exists(databaseName))
                SQLiteConnection.CreateFile(databaseName);

            OpenConnection();

            if (!TableExists("Registers"))
            {
                string sql = "CREATE TABLE Registers (id INT, text VARCHAR(5000))";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public SQLiteConnection GetConnection()
        {
            return m_dbConnection;
        }

        private void OpenConnection()
        {
            m_dbConnection = new SQLiteConnection("Data Source="+ databaseName + ";Version=3;");
            m_dbConnection.Open();
        }

        private bool TableExists(string tableName)
        {
            Debug.Assert(m_dbConnection != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(tableName));

            var cmd = m_dbConnection.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(*) FROM sqlite_master WHERE name=@TableName";
            var p1 = cmd.CreateParameter();
            p1.DbType = DbType.String;
            p1.ParameterName = "TableName";
            p1.Value = tableName;
            cmd.Parameters.Add(p1);

            var result = cmd.ExecuteScalar();
            return ((long)result) == 1;
        }
    }
}
