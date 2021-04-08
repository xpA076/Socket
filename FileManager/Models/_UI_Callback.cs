using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public delegate void PageUICallback();
    public delegate void PageUIInvokeCallback(Action a);

    public delegate void PageUIConnectionStatusCallback(float success_ratio, Action a);

}
