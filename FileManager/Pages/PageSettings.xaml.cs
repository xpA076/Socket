using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using FileManager.Static;
using FileManager.ViewModels;

namespace FileManager.Pages
{
    /// <summary>
    /// PageSettings.xaml 的交互逻辑
    /// </summary>
    public partial class PageSettings : Page
    {
        private readonly SettingsViewModel SettingsView = new SettingsViewModel();

        private Dictionary<string, RoutedEventHandler> ButtonEvents = new Dictionary<string, RoutedEventHandler>();


        public PageSettings()
        {
            InitializeComponent();
            this.DataContext = SettingsView;
            ButtonEvents.Add("ButtonUpdateLengthThreshold", ButtonUpdateLengthThreshold_Click);
            ButtonEvents.Add("ButtonUpdateTimeThreshold", ButtonUpdateTimeThreshold_Click);
            ButtonEvents.Add("ButtonDefaultPort", ButtonDefaultPort_Click);
            ButtonEvents.Add("ButtonSocketSendTimeout", ButtonSocketSendTimeout_Click);
            ButtonEvents.Add("ButtonSocketReceiveTimeout", ButtonSocketReceiveTimeout_Click);
            ButtonEvents.Add("ButtonSmallFileThreshold", ButtonSmallFileThreshold_Click);
            ButtonEvents.Add("ButtonThreadLimit", ButtonThreadLimit_Click);
            ButtonEvents.Add("ButtonSaveRecordInterval", ButtonSaveRecordInterval_Click);
            ButtonEvents.Add("ButtonConnectionMonitorRecordCount", ButtonConnectionMonitorRecordCount_Click);
            ButtonEvents.Add("ButtonConnectionMonitorRecordInterval", ButtonConnectionMonitorRecordInterval_Click);


        }

        public void PageSettings_Loaded(object sender, RoutedEventArgs e)
        {
            this.ButtonUpdateLengthThreshold.Visibility = Visibility.Hidden;
            this.ButtonUpdateTimeThreshold.Visibility = Visibility.Hidden;
            this.ButtonDefaultPort.Visibility = Visibility.Hidden;
            this.ButtonSocketSendTimeout.Visibility = Visibility.Hidden;
            this.ButtonSocketReceiveTimeout.Visibility = Visibility.Hidden;
            this.ButtonSmallFileThreshold.Visibility = Visibility.Hidden;
            this.ButtonThreadLimit.Visibility = Visibility.Hidden;
            this.ButtonSaveRecordInterval.Visibility = Visibility.Hidden;
            this.ButtonConnectionMonitorRecordCount.Visibility = Visibility.Hidden;
            this.ButtonConnectionMonitorRecordInterval.Visibility = Visibility.Hidden;

        }


        public void ClickCloseButtonAction_Checked(object sender, RoutedEventArgs e)
        {
            if (this.ClickCloseButtonActionMinimize.IsChecked == true)
            {
                Config.Instance.ClickCloseToMinimize = true;
            }
            else
            {
                Config.Instance.ClickCloseToMinimize = false;
            }
            Config.Instance.SaveConfig();
            // 这里设为 true 和 false没区别, 只要调用了SettingsView 的 Setter
            SettingsView.ClickCloseToMinimize = true; 
            SettingsView.ClickCloseToClose = true;
            this.NullTextBox.Focus(); 
        }




        public void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            //Button bt = (Button)this.FindName();
            if (e.Key == Key.Enter)
            {
                ButtonEvents[tb.Name.Replace("TextBox", "Button")](null, null);
            }
        }

        public void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Button bt = this.FindName((sender as TextBox).Name.Replace("TextBox", "Button")) as Button;
            bt.Visibility = Visibility.Visible;
        }

        private void RefreshLine(string name)
        {
            SettingsView.GetType().GetProperty(name).SetValue(SettingsView, "");
            TextBox tb = this.FindName("TextBox" + name) as TextBox;
            Button bt = this.FindName("Button" + name) as Button;
            tb.SelectionStart = tb.Text.Length;
            bt.Visibility = Visibility.Hidden;
            /// lose focus
            this.NullTextBox.Focus();
            //tb.Focus();
        }


        private void ButtonUpdateLengthThreshold_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (SettingsViewModel.TryParseBytesString(this.TextBoxUpdateLengthThreshold.Text, out long l))
            {
                if (l > 0)
                {
                    Config.Instance.UpdateLengthThreshold = l;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input syntax, required : [<num>T]?[<num>G]?[<num>M]?[<num>K]?<num>");
            }
            RefreshLine("UpdateLengthThreshold");
        }

        private void ButtonUpdateTimeThreshold_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxUpdateTimeThreshold.Text, out int i))
            {
                if (i > 50)
                {
                    Config.Instance.UpdateTimeThreshold = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] Update UI interval > 50ms");
            }
            RefreshLine("UpdateTimeThreshold");

        }

        private void ButtonDefaultPort_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxDefaultPort.Text, out int i))
            {
                if (i >= 1 && i <= 65535)
                {
                    Config.Instance.DefaultServerPort = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : port number must between 1~65535");
            }
            RefreshLine("DefaultPort");

        }

        public void ButtonSocketSendTimeout_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxSocketSendTimeout.Text, out int i))
            {
                if (i > 500)
                {
                    Config.Instance.SocketSendTimeout = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] SocketSendTimeout > 500ms");
            }
            RefreshLine("SocketSendTimeout");
        }


        private void ButtonSocketReceiveTimeout_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxSocketReceiveTimeout.Text, out int i))
            {
                if (i > 500)
                {
                    Config.Instance.SocketReceiveTimeout = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] SocketReceiveTimeout > 500ms");
            }
            RefreshLine("SocketReceiveTimeout");
        }

        private void ButtonSmallFileThreshold_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (SettingsViewModel.TryParseBytesString(this.TextBoxSmallFileThreshold.Text, out long l))
            {
                if (l > 0)
                {
                    Config.Instance.SmallFileThreshold = l;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input syntax, required : [<num>T]?[<num>G]?[<num>M]?[<num>K]?<num>");
            }
            RefreshLine("SmallFileThreshold");
        }

        private void ButtonThreadLimit_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxThreadLimit.Text, out int i))
            {
                if (i >= 1 && i <= 1024)
                {
                    Config.Instance.ThreadLimit = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : thread limit must between 1~1024");
            }
            RefreshLine("ThreadLimit");
        }

        private void ButtonSaveRecordInterval_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxSaveRecordInterval.Text, out int i))
            {
                if (i > 50)
                {
                    Config.Instance.SaveRecordInterval = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] SaveRecordInterval > 50ms");
            }
            RefreshLine("SaveRecordInterval");
        }

        private void ButtonConnectionMonitorRecordCount_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxConnectionMonitorRecordCount.Text, out int i))
            {
                if (i > 0)
                {
                    Config.Instance.ConnectionMonitorRecordCount = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] ConnectionMonitorRecordCount > 0");
            }
            RefreshLine("ConnectionMonitorRecordCount");
        }

        private void ButtonConnectionMonitorRecordInterval_Click(object sender, RoutedEventArgs e)
        {
            bool valid_flag = false;
            if (int.TryParse(this.TextBoxConnectionMonitorRecordInterval.Text, out int i))
            {
                if (i >= 20)
                {
                    Config.Instance.ConnectionMonitorRecordInterval = i;
                    Config.Instance.SaveConfig();
                    valid_flag = true;
                }
            }
            if (!valid_flag)
            {
                MessageBox.Show("Invalid input : [required] TextBoxConnectionMonitorRecordInterval >= 20");
            }
            RefreshLine("ConnectionMonitorRecordInterval");
        }

        private void ButtonOpenConfigPath_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.StandardInput.WriteLine("C:\\Windows\\explorer.exe " + Config.ConfigDir + "&exit");
            p.StandardInput.AutoFlush = true;
            p.WaitForExit();
            p.Close();
        }
    }
}
 