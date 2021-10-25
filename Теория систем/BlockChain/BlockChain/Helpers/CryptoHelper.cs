using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BlockChain.Entities;
using BlockChain.Models;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace BlockChain.Helpers
{
    public static class CryptoHelper
    {
        public static string CreateHash(string data)
        {
            IDigest hash = new Sha256Digest();
            var result = new byte[hash.GetDigestSize()];
            var dataBytes = Encoding.UTF8.GetBytes(data);
            hash.BlockUpdate(dataBytes, 0, dataBytes.Length);
            hash.DoFinal(result, 0);
            return BitConverter.ToString(result).Replace("-", "");
        }
        
        public static string CreateHash(string data, Block previousBlock)
        {
            data += previousBlock.Hash;
            IDigest hash = new Sha256Digest();
            var result = new byte[hash.GetDigestSize()];
            var dataBytes = Encoding.UTF8.GetBytes(data);
            hash.BlockUpdate(dataBytes, 0, dataBytes.Length);
            hash.DoFinal(result, 0);
            return BitConverter.ToString(result).Replace("-", "");
        }

        public static void GenerateKeys(string privateKeyPath, string publicKeyPath)
        {
            var generator = new ECKeyPairGenerator("ECDH");
            var secureRandom = new SecureRandom();
            var ecp = NistNamedCurves.GetByName("P-256");
            var ecSpec = new ECDomainParameters(ecp.Curve, ecp.G, ecp.N, ecp.H, ecp.GetSeed());
            var ecKeyGenerationParameters = new ECKeyGenerationParameters(ecSpec, secureRandom);
            generator.Init(ecKeyGenerationParameters);
            var ecKeyPair = generator.GenerateKeyPair();

            var ecPublic = (ECPublicKeyParameters) ecKeyPair.Public;
            var ecPrivate = (ECPrivateKeyParameters) ecKeyPair.Private;

            var privateKey = BitConverter
                .ToString(PrivateKeyInfoFactory.CreatePrivateKeyInfo(ecPrivate).GetEncoded(Encoding.UTF8.EncodingName))
                .Replace("-", "");
            var publicKey = BitConverter
                .ToString(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(ecPublic)
                    .GetEncoded(Encoding.UTF8.EncodingName)).Replace("-", "");
            publicKey = Regex.Replace(publicKey, @",\r?\n", "");
            privateKey = Regex.Replace(privateKey, @",\r?\n", "");
            
            using (var sw = new StreamWriter(privateKeyPath, false, Encoding.Default))
            {
                sw.WriteLine(privateKey);
            }

            using (var sw = new StreamWriter(publicKeyPath, false, Encoding.Default))
            {
                sw.WriteLine(publicKey);
            }
        }

        public static string SignData(string msg, ECPrivateKeyParameters privKey)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);

                ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(true, privKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                byte[] sigBytes = signer.GenerateSignature();

                return Convert.ToBase64String(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Signing Failed: " + exc);
                return null;
            }
        }

        public static bool VerifySignature(ECPublicKeyParameters pubKey, string signature, string msg)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                byte[] sigBytes = Convert.FromBase64String(signature);

                ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(false, pubKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Verification failed with the error: " + exc);
                return false;
            }
        }

        public static ECPublicKeyParameters GetPublicKey(string path)
        {
            var publicKey = GetPublicKeyString(path);

            return (ECPublicKeyParameters)PublicKeyFactory.CreateKey(StringToByteArray(publicKey));
        }

        public static ECPrivateKeyParameters GetPrivateKey(string path)
        {
            var privateKey = GetPrivateKeyString(path);

            return (ECPrivateKeyParameters)PrivateKeyFactory.CreateKey(StringToByteArray(privateKey));
        }

        public static string GetPrivateKeyString(string path)
        {
            try
            {
                using var sr = new StreamReader(path);
                var privateKey = sr.ReadToEnd();

                return privateKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public static string GetPublicKeyString(string path)
        {
            try
            {
                using var sr = new StreamReader(path);
                var publicKey = sr.ReadToEnd();
                return publicKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
        
        private static byte[] StringToByteArray(string hex) {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => ToByte(hex.Substring(x, 2)))
                .ToArray();
        }

        private static byte ToByte(string hex)
        {
            try
            {
                return Convert.ToByte(hex, 16);
            }
            catch (Exception e)
            {
                return Convert.ToByte("0", 16);
            }
        }

        public static bool VerifyBlock(Block currentBlock, Block previousBlock, string publicKeyPath)
        {
            var hash = CreateHash(currentBlock.Data, previousBlock);
            var publicKey = GetPublicKey(publicKeyPath);
            var isSign = VerifySignature(publicKey,currentBlock.Signature,currentBlock.Data);
            return currentBlock.Hash == hash && isSign;
        }
        public static bool VerifyBlock(Block currentBlock, string publicKeyPath)
        {
            var hash = CreateHash(currentBlock.Data);
            var publicKey = GetPublicKey(publicKeyPath);
            var isSign = VerifySignature(publicKey,currentBlock.Signature,currentBlock.Data);
            return currentBlock.Hash == hash && isSign;
        }
    }
}