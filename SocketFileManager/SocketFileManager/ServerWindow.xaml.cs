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

namespace SocketFileManager
{
    /// <summary>
    /// ServerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ServerWindow : Window
    {
        public ServerWindow()
        {
            InitializeComponent();
            this.Topbar.MouseDown += new MouseButtonEventHandler(Topbar_MouseDown);
            this.WindowMinimize.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowMinimize_MouseLeftDown);
            this.WindowClose.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(WindowClose_MouseLeftDown);
        }

        #region 标题栏行为
        private void Topbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void WindowMinimize_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WindowClose_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
