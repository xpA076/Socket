using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileManager.Models.Serializable;
using FileManager.SocketLib;
using FileManager.SocketLib.Enums;
using FileManager.Static;

namespace FileManager.Models.TransferLib
{
    /// <summary>
    /// 处理单个 TransferInfoRoot, 调度安排其传输任务
    /// 每个 Dispatcher 对应一个 TransferInfoRoot, 在 Pages 中被事件调用
    /// 每个 TransferInfoRoot 对应的目录树在传输过程中, 同时只能有一个文件处于正在传输状态
    /// </summary>
    public partial class TransferDispatcher
    {

        #region Properties

        private TransferInfoRoot RootInfo;


        private bool IsTransfering = false;


        /// <summary>
        /// 目录路径对应 DFS 缓存栈
        /// </summary>
        private readonly Stack<int> DirectoryIndexStack = new Stack<int>();
        private TransferInfoDirectory CurrentDirectoryInfo = null;
        private int IndexFile = 0;


        private TransferDiskManager DiskManager = null;
        private IndexGenerator IndexGenerator = null;

        #endregion

        public TransferDispatcher(TransferInfoRoot rootInfo)
        {
            RootInfo = rootInfo;
            CurrentDirectoryInfo = rootInfo;
        }

        public void InitTransfer()
        {
            Task.Run(() => { TransferMain(); });
        }

        private void TransferMain()
        {
            IsTransfering = true;
            if (RootInfo.Type == TransferType.Download)
            { 
                DownloadMain();
            }
            IsTransfering = false;
        }

        private void DownloadMain()
        {
            /// todo 在这里确认当前任务已经 Query 完成
            /// 先不搞 Query 和传输异步那些
            /// 对同一个网络路径的数据通信, 没必要通过异步提升效率
            /// /todo

            /// --------
            /// 以文件为单位进行主循环
            /// (以后优化可以将目录树序列化, 以块为单位进行主循环)
            while (true)
            {
                /// 将 Stack 和 CurrentDirectoryInfo 指向正确位置
                if (!MovePointerToFirstFile()) { break; }
                /// 当前任务传输过程
                TransferInfoFile infoFile = CurrentDirectoryInfo.FileChildren[IndexFile];
                DownloadOneFile(infoFile);
                /// 标记当前 File 任务完成
                CurrentDirectoryInfo.TransferCompleteFiles[IndexFile] = true;
            }
        }

        public void DownloadSmallFile(TransferInfoFile infoFile)
        { 
            DownloadRequest request = new DownloadRequest()
            {
                Type = DownloadRequest.RequestType.SmallFile,
                ViewPath = infoFile.RemotePath
            };
            HB32Response hb_response;
            try
            {
                hb_response = SocketFactory.Instance.Request(HB32Packet.DownloadRequest, request.ToBytes());
            }
            catch (Exception)
            {
                // todo Server端 Socket 通信异常
                throw new NotImplementedException();
            }
            DownloadResponse response = DownloadResponse.FromBytes(hb_response.Bytes);
            if (response.Type == DownloadResponse.ResponseType.BytesResponse)
            {
                File.WriteAllBytes(infoFile.LocalPath, response.Bytes);
            }
            else
            {
                // todo Server端 返回内部异常 (权限问题等, 无 socket 异常)
                throw new NotImplementedException();
            }
        }


        private void DownloadOneFile(TransferInfoFile infoFile)
        {
            int thread_count = 1;
            if (infoFile.Length > (16 << 10))
            {
                thread_count = 10;
            }



        }


        private void DownloadThreadUnit()
        {

        }



        /// <summary>
        /// 从 CurrentDirectoryInfo 开始, DFS查找下一个未传输文件位置
        /// 查找路径上经过的未建立目录的路径, 会建立对应目录
        /// 并将路径上所有目录上节点 TransferInfoDirectory 正确标注
        /// </summary>
        /// <returns>是否成功移动至下一个文件节点, 若返回 false 证明已经全部传输完成</returns>
        private bool MovePointerToFirstFile()
        {
            /// 因为保证整个目录树 Query 完成才能开始传输
            /// 因此调用本函数时, 当前节点的子目录树应该已构建完成, 不考虑目录未构建问题
            while (true)
            {
                /// 按顺序尝试进入当前 Directory 的未完成子目录, 若成功则在子目录重复该循环
                for (int i = 0; i < CurrentDirectoryInfo.DirectoryChildren.Count; ++i)
                {
                    if (!CurrentDirectoryInfo.TransferCompleteDirectories[i])
                    {
                        DirectoryIndexStack.Push(i);
                        CurrentDirectoryInfo = CurrentDirectoryInfo.DirectoryChildren[i];
                        /// 若成功进入当前子节点, 先建立文件夹, 而后在子目录中重复循环
                        if (!Directory.Exists(CurrentDirectoryInfo.LocalPath))
                        {
                            Directory.CreateDirectory(CurrentDirectoryInfo.LocalPath);
                        }
                        continue;
                    }
                }
                /// 未成功进入子目录, 则尝试获取子节点中的未完成文件
                for (int i = 0; i < CurrentDirectoryInfo.FileChildren.Count; ++i)
                {
                    if (!CurrentDirectoryInfo.TransferCompleteFiles[i])
                    {
                        IndexFile = i;
                        return true;
                    }
                }
                /// 未获取到本级中的未完成文件
                ///  if    当前节点为 Root, 说明已经完成所有文件
                ///  else  回溯至上级, 将上级中本节点标记为完成, 在上级中重复查找 File 节点
                if (CurrentDirectoryInfo.IsRoot)
                {
                    /// 已回溯至 Root 节点, 所有文件已经完成
                    return false;
                }
                int idx = DirectoryIndexStack.Pop();
                CurrentDirectoryInfo.Parent.TransferCompleteDirectories[idx] = true;
                CurrentDirectoryInfo = CurrentDirectoryInfo.Parent;
            }
        }



        private bool SetTransferSession(string path, TransferType transferType)
        {
            

            return false;
        }

        
        private void RunSubThreads()
        {

        }


        private void TransferThreadUnit()
        {

            

        }





        private bool IsTransferComplete()
        {
            // todo

            return false;
        }

    }
}
