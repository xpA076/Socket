using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketFileManager.Models
{
    public class ProgressViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string current;
        private string total;
        private string speed;
        private string time;

        public string CurrentProgress
        {
            set
            {
                current = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProgress"));
            }
            get
            {
                return current;
            }
        }

        public string TotalProgress
        {
            set
            {
                total = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalProgress"));
            }
            get
            {
                return total;
            }
        }

        public string Speed
        {
            set
            {
                speed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Speed"));
            }
            get
            {
                return speed;
            }
        }

        public string TimeRemaining
        {
            set
            {
                time = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TimeRemaining"));
            }
            get
            {
                return time;
            }
        }

    }
}
