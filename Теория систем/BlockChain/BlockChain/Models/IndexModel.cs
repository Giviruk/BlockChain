using System.Collections.Generic;

namespace BlockChain.Models
{
    public class IndexModel
    {
        public List<BlockViewModel> Blocks { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        
    }
}