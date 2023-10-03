﻿using FileManager.SocketLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.Serializable.Crypto
{
    public class KeyExchangeResponse : ISocketSerializable
    {
        public byte[] PublicKey { get; set; }

        public static KeyExchangeResponse FromBytes(byte[] bytes)
        {
            int idx = 0;
            KeyExchangeResponse obj = new KeyExchangeResponse();
            obj.BuildFromBytes(bytes, ref idx);
            return obj;
        }

        public void BuildFromBytes(byte[] bytes, ref int idx)
        {
            this.PublicKey = BytesParser.GetBytes(bytes, ref idx);
        }

        public byte[] ToBytes()
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Append(PublicKey);
            return bb.GetBytes();
        }
    }
}
