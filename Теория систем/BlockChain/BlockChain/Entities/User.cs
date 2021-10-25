using Microsoft.AspNetCore.Identity;

namespace BlockChain.Entities
{
    public class User : IdentityUser
    {
        public string PublicSignature { get; protected set; }
        
        public string PrivateSignature { get; protected set; }
    }
}