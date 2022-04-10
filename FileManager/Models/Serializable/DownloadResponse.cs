﻿using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable
{
    public class DownloadResponse : ISocketSerializable
    {
        public enum ResponseType : int
        {
            BytesResponse,
            PacketResponse,
            ResponseException
        }

        public ResponseType Type { get; set; }

        public byte[] Bytes { get; set; }

        public static DownloadResponse FromBytes(byte[] bytes)
        {
            DownloadResponse response = new DownloadResponse();
            response.BuildFromBytes(bytes);
            return response;
        }

        public void BuildFromBytes(byte[] bytes)
        {
            int idx = 0;
            this.Type = (ResponseType)BytesParser.GetInt(bytes, ref idx);
            this.Bytes = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append((int)Type);
            bb.Append(Bytes);
            return bb.GetBytes();
        }
    }
}