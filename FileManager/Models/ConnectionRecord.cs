using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models
{
    public class ConnectionRecord : INotifyPropertyChanged
    {
        private bool _is_starred = false;

        public bool IsStarred
        {
            get
            {
                return _is_starred;
            }
            set
            {
                _is_starred = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsStarred"));
            }
        }


        public string Info { get; set; }


        /*
        public string Fav 
        {
            get
            {
                return _star;
            }
            set
            {
                _star = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Fav"));
            }
        }
        */
        public void Star()
        {
            //Fav = "F";
            IsStarred = true;
        }

        public void Unstar()
        {
            //Fav = "&#xe608;";
            IsStarred = false;
        }

        public ConnectionRecord Copy()
        {
            return new ConnectionRecord
            {
                Info = this.Info,
                IsStarred = this.IsStarred
                //Fav = this.Fav
            };
        }

        public void CopyFrom(ConnectionRecord connectionRecord)
        {
            this.Info = connectionRecord.Info;
            this.IsStarred = connectionRecord.IsStarred;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
