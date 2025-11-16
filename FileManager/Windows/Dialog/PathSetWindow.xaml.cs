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
using System.Windows.Shapes;

using FileManager.Events;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace FileManager.Windows.Dialog
{
    /// <summary>
    /// PathSetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PathSetWindow : Window
    {
        private const string DefaultPrompt = "Write path here ...";

        private bool IsChecking = false;

        public event CheckPathEventHandler CheckPathCallback;

        public string Path;

        public PathSetWindow()
        {
            InitializeComponent();
            this.TextBoxPath.Text = DefaultPrompt;
        }

        private void WindowClose_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TextBoxPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonSet_ClickAsync(null, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSet_ClickAsync(object sender, RoutedEventArgs e)
        {
            Task<bool> f(string path)
            {
                return Task.Run(() =>
                {
                    var ce = new CheckPathEventArgs(path);
                    CheckPathCallback(this, ce);
                    return ce.IsPathValid;
                });
            };

            if (IsChecking) { return; }
            this.IsChecking = true;
            this.TextBlockChecking.Visibility = Visibility.Visible;
            bool path_flag = await f(this.TextBoxPath.Text);
            if (path_flag)
            {
                this.Path = this.TextBoxPath.Text;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                this.IsChecking = false;
                this.TextBlockChecking.Visibility = Visibility.Hidden;
                MessageBox.Show("Path invalid");
            }
        }



        private void TextBoxPath_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.TextBoxPath.Text == DefaultPrompt)
            {
                this.TextBoxPath.Text = "";
            }
        }

        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
