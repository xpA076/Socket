using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Events
{
    public delegate void CheckPathEventHandler(object sender, CheckPathEventArgs e);

    public class CheckPathEventArgs : EventArgs
    {
        public string Path { get; set; }

        public bool IsPathValid { get; set; } = false;

        public CheckPathEventArgs()
        {

        }

        public CheckPathEventArgs(string path)
        {
            Path = path;
        }
    }
}
