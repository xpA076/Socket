using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FileManager.Windows
{
    public enum UploadChoose
    {
        None = 0,
        Files = 1,
        Folder = 2
    }
    /// <summary>
    /// UploadSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UploadSelectWindow : Window
    {
        private FolderBrowserDialog folderDialog = new FolderBrowserDialog();
        private OpenFileDialog fileDialog = new OpenFileDialog();

        public string DisplayPath
        {
            set
            {
                this.TextMain.Text = "Are you sure to upload file in \"" + value + "\" ?";
            }
        }

        public UploadChoose UploadChoosen { get; private set; }
        public List<string> UploadPathList { get; private set; } = new List<string>();
        

        public UploadSelectWindow()
        {
            InitializeComponent();
            this.Topbar.MouseDown += new MouseButtonEventHandler(Topbar_MouseDown);
            /// buttons
            this.WindowClose.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(CloseWindow);
            this.ButtonUploadFile.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonUploadFile_Click);
            this.ButtonUploadFolder.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(ButtonUploadFolder_Click);
            this.ButtonCancel.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(CloseWindow);
            /// dialog
            fileDialog.Multiselect = true;
        }


        private void ButtonUploadFile_Click(object sender, MouseButtonEventArgs e)
        {
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UploadPathList.Clear();
                foreach (string localPath in fileDialog.FileNames)
                {
                    UploadPathList.Add(localPath);
                }
                this.DialogResult = true;
                UploadChoosen = UploadChoose.Files;
            }
            else { this.DialogResult = false; }
            this.Close();
        }


        private void ButtonUploadFolder_Click(object sender, MouseButtonEventArgs e)
        {
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UploadPathList.Clear();
                UploadPathList.Add(folderDialog.SelectedPath);
                this.DialogResult = true;
                UploadChoosen = UploadChoose.Folder;
            }
            else { this.DialogResult = false; }
            this.Close();
        }


        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        private void CloseWindow(object sender, EventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }


        public void Clear()
        {
            this.UploadChoosen = UploadChoose.None;
            this.UploadPathList.Clear();
        }
    }
}
