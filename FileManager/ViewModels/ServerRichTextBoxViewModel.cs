using FileManager.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.ViewModels
{
    public class ServerRichTextBoxViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 用这种方式实现 RichTextBox 界面更新.
        /// 因为 SocketServer 的 Log 操作多在新建线程中, 没有在 WPF 更新 UI 的权限.
        /// 通过将 Nothing 数据绑定到面板某一不可见UI (注册 PropertyChanged 事件):
        ///   其它线程执行 InvokeLog 方法时, 会调用 UI 更新线程执行 Nothing 的 get 方法,
        ///   在 get 方法中更新 RichTextBox 中的 FlowDocument 没有权限限制.
        /// </summary>
        public delegate void RichTextBoxUpdateEventHandler(object sender, SocketLogEventArgs e);

        public event RichTextBoxUpdateEventHandler RichTextBoxUpdate;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Nothing
        {
            get
            {
                RichTextBoxUpdate(this, last_event_args);
                return "";
            }
        }

        SocketLogEventArgs last_event_args;

        public void InvokeLog(SocketLogEventArgs e)
        {
            last_event_args = e;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Nothing"));
        }
    }
}
