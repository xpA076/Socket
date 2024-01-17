using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Utils.Storage
{
    public sealed class ClientConfigStorage
    {
        private struct InfoStruct
        {
            public bool ClickCloseToMinimize;
            public long UpdateLengthThreshold;
            public int UpdateTimeThreshold;
        }

        private InfoStruct Info = new InfoStruct();

        private readonly StoragePathMapper PathMapper = StoragePathMapper.Instance;

        private static readonly Lazy<ClientConfigStorage> _instance = new Lazy<ClientConfigStorage>(() => new ClientConfigStorage());

        public static ClientConfigStorage Instance {  get { return _instance.Value; } }

        private ClientConfigStorage()
        {
            Info.ClickCloseToMinimize = true;
        }

        /// <summary>
        /// 窗口关闭操作
        /// </summary>
        public bool ClickCloseToMinimize
        {
            get { return Info.ClickCloseToMinimize;  }
            set
            {
                Info.ClickCloseToMinimize = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// 刷新界面所需传输最小字节数
        /// </summary>
        public long UpdateLengthThreshold 
        {
            get { return Info.UpdateLengthThreshold; }
            set { 
                Info.UpdateLengthThreshold = value;
                SaveConfig();
            }
        }

        public int UpdateTimeThreshold
        {
            get { return Info.UpdateTimeThreshold; }
            set
            {
                Info.UpdateTimeThreshold = value;
                SaveConfig();
            }
        }



        public void LoadConfig()
        {

        }

        public void SaveConfig()
        {

        }
    }
}
