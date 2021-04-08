using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.Models;
using FileManager.Static;

namespace FileManager.ViewModels
{
    public class ConnectionStatusViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _color_str = "#aaa";

        public string Color
        {
            get
            {
                return _color_str;
            }
        }

        private string _tooltip_str = "not monitoring";

        public string ToolTip
        {
            get
            {
                return _tooltip_str;
            }
        }


        public double[] RatioThreshold { get; set; } = new double[] { 0.5, 0.99 };

        public string ColorInactive = "#aaa";
        public string[] ColorStrings { get; set; } = new string[] { "#0e0", "#ee0", "e00" };

        public void SetStatus(HeartBeatConnectionMonitor monitor)
        {
            if (monitor.StatusRecords.Count == 0)
            {
                _color_str = "#aaa";
                _tooltip_str = "not monitoring";
                
            }
            else
            {
                int success = 0, fail = 0;
                foreach (HeartBeatConnectionStatusRecord rec in monitor.StatusRecords)
                {
                    if (rec.Status)
                    {
                        success++;
                    }
                    else
                    {
                        fail++;
                    }
                }
                double ratio = (double)success / (success + fail);
                if (ratio > 0.95)
                {
                    _color_str = "#0e0";
                    _tooltip_str = "Good : ";
                }
                else if (ratio > 0.5)
                {
                    _color_str = "#ee0";
                    _tooltip_str = "Warn : ";
                }
                else
                {
                    _color_str = "#e00";
                    _tooltip_str = "Fail : ";
                }

                _tooltip_str += string.Format("{0} / {1} connection in last {2}s",
                    success, success + fail, monitor.DifSeconds);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToolTip"));


        }
    }
}
