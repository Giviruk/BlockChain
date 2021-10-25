using Org.BouncyCastle.Crypto.Parameters;

namespace BlockChain.Models
{
    public class KeyPair
    {
        public ECPrivateKeyParameters PrivateKey { get; set; }
        public ECPublicKeyParameters PublicKey { get; set; }
    }
}