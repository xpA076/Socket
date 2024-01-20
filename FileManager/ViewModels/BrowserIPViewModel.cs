using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.Static;
using FileManager.Models.SocketLib.Models;

namespace FileManager.ViewModels
{
    public class BrowserIPViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private TCPAddress server_address = new TCPAddress();

        public TCPAddress ServerAddress
        {
            get
            {
                return server_address;
            }
            set
            {
                server_address = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ServerAddressStr"));
            }
        }


        public string ServerAddressStr
        {
            get
            {
                if (ServerAddress.IP == null)
                {
                    return "Connected IP - none";
                }
                else
                {
                    return string.Format("Connected IP - {0}{1}", server_address.IP.ToString(),
                        (server_address.Port == Config.Instance.DefaultServerPort) ? "" : (":" + server_address.Port.ToString()));
                }
            }
        }
    }
}
