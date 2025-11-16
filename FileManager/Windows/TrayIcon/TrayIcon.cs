using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Application = System.Windows.Application;

namespace FileManager.Windows.TrayIcon
{
    public class TrayIcon : IDisposable
    {
        private NotifyIcon _notifyIcon;

        public TrayIcon()
        {
            _notifyIcon = new NotifyIcon();
        }

        public void Initialize()
        {
            // 设置托盘图标
            _notifyIcon.Icon = LoadIcon();

            _notifyIcon.Text = "FileManager";

            // 添加右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, OnShowWindow);
            contextMenu.Items.Add("Exit", null, OnExit);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 绑定事件
            _notifyIcon.DoubleClick += OnDoubleClick;
            _notifyIcon.Visible = true;
        }


        private Icon LoadIcon()
        {
            try
            {
                Uri resourceUri = new Uri("Resources/icon.ico", UriKind.Relative);
                StreamResourceInfo streamInfo = Application.GetResourceStream(resourceUri);

                if (streamInfo != null)
                {
                    using (Stream stream = streamInfo.Stream)
                    {
                        return new System.Drawing.Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从资源加载图标失败: {ex.Message}");
            }

            // 回退到系统图标
            return SystemIcons.Application;
        }

        private void OnDoubleClick(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnShowWindow(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowMainWindow()
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.Activate();
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
