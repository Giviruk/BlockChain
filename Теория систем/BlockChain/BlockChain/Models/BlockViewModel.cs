namespace BlockChain.Models
{
    public class BlockViewModel
    {
        public int BlockNumber { get; set; }
        public string Data { get; set; }
        public string Hash { get; set; }
        public string Signature { get; set; }
        public string Verified { get; set; }
    }
}