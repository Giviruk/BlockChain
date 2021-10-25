using System;

namespace BlockChain.Entities
{
    public class Block
    {
        public Guid Id { get; protected set; }
        public int BlockNumber { get; protected set; }
        public string Data { get; protected set; }
        public string Hash { get; protected set; }
        public string Signature { get; protected set; }
        
        public DateTime CreationDate { get; protected set; }

        public Block(int blockNumber, string data, string hash, string signature, DateTime creationDate)
        {
            BlockNumber = blockNumber;
            Data = data;
            Hash = hash;
            Signature = signature;
            CreationDate = creationDate;
        }

        protected Block()
        {
            
        }
    }
}