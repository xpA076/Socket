﻿using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class ReleaseFileRequest : ISocketSerializable
    {
        public enum RequestType : int
        {
            Default,
        }


        public enum ReleaseFrom : int
        {
            Download,
            Upload
        }

        public RequestType Type { get; set; } = RequestType.Default;

        public ReleaseFrom From { get; set; }

        public string ViewPath { get; set; }

        public ReleaseFileRequest()
        {

        }


        public static ReleaseFileRequest FromBytes(byte[] bytes)
        {
            int idx = 0;
            ReleaseFileRequest obj = new ReleaseFileRequest();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }


        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append((int)From);
            bb.Append(ViewPath);
            return bb.GetBytes();
        }


        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.Type = (RequestType)BytesParser.GetInt(bytes, ref idx);
            this.From = (ReleaseFrom)BytesParser.GetInt(bytes, ref idx);
            this.ViewPath = BytesParser.GetString(bytes, ref idx);
        }

    }
}
