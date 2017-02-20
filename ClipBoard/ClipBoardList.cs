using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ClipBoard
{
    public partial class ClipBoardList : Form
    {
        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        IntPtr nextClipboardViewer;

        SQLiteConnection m_dbConnection;

        List<ClipboardFile> list;

        public List<ClipboardFile> ListClipboardFile
        {
            get
            {
                if (list == null)
                    list = new List<ClipboardFile>();

                return list;
            }
        }

        public ClipBoardList()
        {
            InitializeComponent();
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
            WindowState = FormWindowState.Minimized;

            if (!File.Exists("MyDatabase.sqlite"))
                SQLiteConnection.CreateFile("MyDatabase.sqlite");

            OpenConnection();

            if (!TableExists("Registers"))
            {
                string sql = "CREATE TABLE Registers (id INT, text VARCHAR(5000))";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
            }

            LoadClipboardList();
        }

        private void Configuration_Resize(object sender, System.EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void OpenConnection()
        {
            m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            m_dbConnection.Open();
        }

        private void ShowForm()
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void InsertText(string text)
        {
            VerifyIfExistsText(text);
            var id = IdIncrement();
            string sql = "insert into Registers (id, text) values (" + id.ToString() + ", '" + text + "')";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        private int IdIncrement()
        {
            string sql = "SELECT MAX(id) FROM Registers";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            var newId = Convert.ToInt32(command.ExecuteScalar()) + 1;
            return newId;
        }

        private void VerifyIfExistsText(string text)
        {
            string sql = "SELECT id FROM Registers WHERE text='"+ text +"'";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            var id = Convert.ToInt32(command.ExecuteScalar());

            if (id > 0)
                DeleteRegister(id);
        }

        private void DeleteRegister(int id)
        {
            string sql = "DELETE FROM Registers WHERE id=" + id.ToString() + "";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public bool TableExists(string tableName)
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

        private void LoadClipboardList()
        {
            string sql = "SELECT * FROM Registers";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            int contador = 1;

            while (reader.Read())
            {
                var register = new ClipboardFile();
                register.Id = (int)reader["id"];
                register.Description = (string)reader["text"];
                register.Order = contador;
                contador++;
                ListClipboardFile.Add(register);
            }

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGridView1.DataSource = ListClipboardFile;
        }

        private void toolStripMenuOpen_Click(object sender, System.EventArgs e)
        {
            ShowForm();
        }

        private void toolStripMenuClose_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        protected override void WndProc(ref Message m)
        {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    DisplayClipboardData();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        void DisplayClipboardData()
        {
            try
            {
                IDataObject iData = new DataObject();
                iData = Clipboard.GetDataObject();

                if (m_dbConnection == null)
                    OpenConnection();

                if (iData.GetDataPresent(DataFormats.Text))
                    InsertText((string)iData.GetData(DataFormats.Text));

                //if (iData.GetDataPresent(DataFormats.Rtf))
                //    richTextBox1.Rtf = (string)iData.GetData(DataFormats.Rtf);
                //else if (iData.GetDataPresent(DataFormats.Text))
                //    richTextBox1.Text = (string)iData.GetData(DataFormats.Text);
                //else
                //    richTextBox1.Text = "[Clipboard data is not RTF or ASCII Text]";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }
    }
}
