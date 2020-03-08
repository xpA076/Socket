using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketFileManager.SocketLib
{
    public class SokcetFileClass
    {
        public string Name { get; set; }
        public long Length { get; set; } = 0;
        public bool IsDirectory { get; set; } = false;

        [System.Web.Script.Serialization.ScriptIgnore]
        public string Size
        {
            get
            {
                if (IsDirectory) { return ""; }
                if ((Length / (1 << 30)) > 0)
                {
                    double size = (double)(Length >> 20) / 1024;
                    return size.ToString("0.00") + " G";
                }
                else if((Length / (1 << 20)) > 0)
                {
                    double size = (double)(Length >> 10) / 1024;
                    return size.ToString("0.00") + " M";
                }
                else if ((Length / (1 << 10)) > 0)
                {
                    double size = (double)Length / 1024;
                    return size.ToString("0.00") + " K";
                }
                else
                {
                    return Length.ToString() + " B";
                }

            }
        }

        public static int Compare(SokcetFileClass f1, SokcetFileClass f2)
        {
            if (f1.IsDirectory == f2.IsDirectory)
            {
                return f1.Name.CompareTo(f2.Name);
            }
            else
            {
                return f1.IsDirectory ? -1 : 1;
            }
        }
    }
}
