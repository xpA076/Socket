﻿using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DownloadRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            SmallFile,
            LargeFile
        }

        public RequestType Type { get; set; }

        public string ServerPath { get; set; }


        public static DownloadRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            DownloadRequest obj = new DownloadRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(ServerPath);
            return bb.GetBytes();
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.ServerPath = BytesParser.GetString(bytes, ref idx);
        }
    }
}
