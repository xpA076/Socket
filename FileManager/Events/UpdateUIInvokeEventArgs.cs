using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events
{
    public delegate void UpdateUIEventHandler(object sender, EventArgs e);

    public delegate void UpdateUIInvokeEventHandler(object sender, UpdateUIInvokeEventArgs e);


    public class UpdateUIInvokeEventArgs : EventArgs
    {
        public readonly Action action;

        public UpdateUIInvokeEventArgs(Action action)
        {
            this.action = action;
        }

    }
}
