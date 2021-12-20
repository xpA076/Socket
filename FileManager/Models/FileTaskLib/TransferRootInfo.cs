using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileManager.SocketLib;
using FileManager.SocketLib.Enums;

namespace FileManager.Models
{
    public class TransferRootInfo : TransferDirectoryInfo
    {
        public TransferRootInfo()
        {
            this.Parent = null;
            this.Name = "";
            this.Querier = new TransferRootInfoQuerier(this);

        }

        #region Parameters to save

        public override int BytesLength
        {
            get
            {
                return 0;
            }
        }

        public ConnectionRoute Route { get; set; }

        public FilterRule Rule { get; set; }

        public TransferType Type { get; set; }


        /// <summary>
        /// 远程目录, 结尾不含 "\\"
        /// </summary>
        public string RemoteDirectory { get; set; }

        /// <summary>
        /// 本地目录, 结尾不含 "\\"
        /// </summary>
        public string LocalDirectory { get; set; }

        #endregion

        #region Parameters
        public TransferRootInfoQuerier Querier = null;


        #endregion




        /// <summary>
        /// DFS 递归保存 TransferRootInfo
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                /// 文件头
                fs.Write(new byte[4] { 0x01, 0x01, 0x01, 0x01 }, 0, 4);
                /// 写入 Root 节点信息
                BytesBuilder bb = new BytesBuilder();
                bb.AppendWithLength(Route.GetBytes());
                bb.AppendWithLength(Rule.GetBytes());
                bb.Append((int)Type);
                bb.Append(RemoteDirectory);
                bb.Append(LocalDirectory);
                bb.Append(Name);
                bb.Append(Length);
                bb.Append(IsChildrenListBuilt);
                bb.Append(QueryCompleteFlags);
                bb.Append(TransferCompleteFlags);
                byte[] bs = bb.GetBytes();
                fs.Write(BitConverter.GetBytes(bs.Length), 0, 4);
                fs.Write(bs, 0, bs.Length);
                /// 写入子节点信息
                fs.Write(BitConverter.GetBytes(DirectoryChildren.Count), 0, 4);
                foreach (var child in DirectoryChildren)
                {
                    child.SaveToFile(fs);
                }
                fs.Write(BitConverter.GetBytes(FileChildren.Count), 0, 4);
                foreach (var child in FileChildren)
                {
                    child.SaveToFile(fs);
                }
            }
        }

        public TransferRootInfo Load(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] hbs = new byte[4];
                fs.Read(hbs, 0, 4);
                if (hbs[0] == 0x01 && hbs[1] == 0x01 && hbs[2] == 0x01 && hbs[3] == 0x01)
                {
                    return _load_211009(fs);
                }
                else
                {
                    return null;
                }
            }
        }

        private TransferRootInfo _load_211009(FileStream fs)
        {
            TransferRootInfo root = new TransferRootInfo();
            /// RootInfo
            byte[] b_len = new byte[4];
            fs.Read(b_len, 0, 4);
            int len = BitConverter.ToInt32(b_len, 0);
            byte[] bs = new byte[len];
            fs.Read(bs, 0, len);
            ///   ConnectionRoute
            int idx = 0;
            byte[] bs0 = BytesParser.GetBytes(bs, ref idx);
            root.Route = ConnectionRoute.FromBytes(bs0);
            ///   FilterRule
            bs0 = BytesParser.GetBytes(bs, ref idx);
            root.Rule = FilterRule.FromBytes(bs0);
            ///   Other properties
            root.Type = (TransferType)BytesParser.GetInt(bs, ref idx);
            root.RemoteDirectory = BytesParser.GetString(bs, ref idx);
            root.LocalDirectory = BytesParser.GetString(bs, ref idx);
            root.Name = BytesParser.GetString(bs, ref idx);
            root.Length = BytesParser.GetLong(bs, ref idx);
            root.IsChildrenListBuilt = BytesParser.GetBool(bs, ref idx);
            root.QueryCompleteFlags = BytesParser.GetListBool(bs, ref idx);
            root.TransferCompleteFlags = BytesParser.GetListBool(bs, ref idx);
            /// 构造子节点
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferDirectoryInfo info = TransferDirectoryInfo.ReadFromFile(fs);
                info.Parent = root;
                root.DirectoryChildren.Add(info);
            }
            fs.Read(b_len, 0, 4);
            len = BitConverter.ToInt32(b_len, 0);
            for (int i = 0; i < len; ++i)
            {
                TransferFileInfo info = TransferFileInfo.ReadFromFile(fs);
                info.Parent = root;
                root.FileChildren.Add(info);
            }
            return root;
        }




    }
}
