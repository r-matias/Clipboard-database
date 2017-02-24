using ClipBoard.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace ClipBoard.Repository
{
    public class ClipboardRepository
    {
        private readonly SQLiteConnection connection;

        public ClipboardRepository()
        {
            var db = new DatabaseConnection();
            connection = db.GetConnection();
        }

        public void Insert(string text)
        {
            GetItemByText(text);
            var id = IdIncrement();
            string sql = "insert into Registers (id, text) values (" + id.ToString() + ", '" + text + "')";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public List<ClipboardFile> GetSelect()
        {
            var list = new List<ClipboardFile>();
            string sql = "SELECT * FROM Registers";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                var register = new ClipboardFile();
                register.Id = (int)reader["id"];
                register.Description = (string)reader["text"];
                list.Add(register);
            }

            int contador = list.Count;
            foreach (var item in list)
            {
                item.Order = contador--;
            }

            list = list.OrderBy(x => x.Order).ToList();

            return list;
        }

        private void GetItemByText(string text)
        {
            string sql = "SELECT id FROM Registers WHERE text='" + text + "'";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            var id = Convert.ToInt32(command.ExecuteScalar());

            if (id > 0)
                Delete(id);
        }

        private void Delete(int id)
        {
            string sql = "DELETE FROM Registers WHERE id=" + id.ToString() + "";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        private int IdIncrement()
        {
            string sql = "SELECT MAX(id) FROM Registers";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            var item = command.ExecuteScalar();

            int newId = 1;

            if (Int32.TryParse(item.ToString(), out newId))
                newId = Convert.ToInt32(item) + 1;
            else
                newId = 1;

            return newId;
        }
    }
}
