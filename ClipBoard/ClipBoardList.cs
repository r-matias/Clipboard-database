using ClipBoard.Model;
using ClipBoard.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private List<ClipboardFile> list;
        private ClipboardRepository repository;

        public List<ClipboardFile> ListClipboardFile
        {
            get
            {
                if (list == null)
                    list = new List<ClipboardFile>();

                return list;
            }
            set
            {
                list = value;
            }
        }

        public ClipBoardList()
        {
            repository = new ClipboardRepository();

            InitializeComponent();
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
            WindowState = FormWindowState.Minimized;

            LoadClipboardList();
        }

        private void Configuration_Resize(object sender, System.EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void ShowForm()
        {
            Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void LoadClipboardList()
        {
            ListClipboardFile = repository.GetSelect();

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGridView1.DataSource = ListClipboardFile;
        }

        private void toolStripMenuOpen_Click(object sender, System.EventArgs e)
        {
            LoadClipboardList();
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
            const int WM_NCATIVATE = 0x86;

            //Hide the form when clicked outside
            if (m.Msg == WM_NCATIVATE && this.ContainsFocus)
                this.Hide();

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

                //if (m_dbConnection == null)
                //    OpenConnection();

                if (iData.GetDataPresent(DataFormats.Text))
                    repository.Insert((string)iData.GetData(DataFormats.Text));

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

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {
            var row = dataGridView1.SelectedRows;

            Clipboard.SetText(row[0].Cells[1].Value.ToString());

            Hide();
        }
    }
}
