using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.TransferLib
{
    public class TransferTaskBlock : IEquatable<TransferTaskBlock>
    {
        public enum BlockContent
        {
            // ?
            SetSession,
            Transfer,


        }

        public enum BlockStatus
        {
            Success,
            SocketException,
            Timeout
        }

        public TransferTaskBlock(int id, int index)
        {
            Id = id;
            Index = index;
        }

        public readonly BlockContent Content;
        public readonly string Path;
        public readonly int Id;
        public readonly int Index;


        public bool Equals(TransferTaskBlock other)
        {
            return Id == other.Id && Index == other.Index;
        }
    };
}
