using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Config
{
    internal class ConfigService
    {
        private readonly ClientConfigStorage clientConfigStorage = Program.Provider.GetService<ClientConfigStorage>();

        public ConfigService() { }  

        public string ConfigDir
        {
            get
            {
                return clientConfigStorage.ConfigDir;
            }
        }

        public bool ClickCloseToMinimize
        {
            get
            {
                return clientConfigStorage.ClickCloseToMinimize;
            }
            set
            {
                clientConfigStorage.ClickCloseToMinimize = value;
                clientConfigStorage.SaveConfig();
            }
        }
        /// <summary>
        /// 刷新界面所需传输最小字节数
        /// </summary>
        public long UpdateLengthThreshold { get; set; } = 128 * 1024;
        /// <summary>
        /// 刷新界面最短时间间隔 (ms)
        /// </summary>
        public int UpdateTimeThreshold { get; set; } = 500;


        #region HeartBeatConnection 相关参数
        public int ConnectionMonitorRecordCount { get; set; } = 10;
        public int ConnectionMonitorRecordInterval { get; set; } = 3000;

        #endregion


        public int DefaultServerPort { get; set; } = 12138;

        public int DefaultProxyPort { get; set; } = 12139;

        public int BuildConnectionTimeout { get; set; } = 2000;

        public int SocketSendTimeout { get; set; } = 5000;

        public int SocketReceiveTimeout { get; set; } = 5000;

        /// <summary>
        /// 启动多线程传输文件大小阈值
        /// </summary>
        public long SmallFileThreshold { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// 文件传输过程每个Packet大小
        /// </summary>
        public long TransferBlockSize { get; set; } = 4096;

        public int ThreadLimit { get; set; } = 16;

        /// <summary>
        /// Transfer 过程中保存 Record 间隔
        /// </summary>
        public int SaveRecordInterval { get; set; } = 5000;

        public byte[] KeyBytes { get; set; } = new byte[256];

    }
}
