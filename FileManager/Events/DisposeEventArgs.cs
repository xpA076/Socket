using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events
{
    public delegate void DisposeEventHandler(object sender, EventArgs e);

    public class DisposeEventArgs : EventArgs
    {
    }
}
