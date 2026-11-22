using System.Security.Cryptography;

namespace WalletApp
{
    public class Wallet
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public byte[] PrivateKey { get; set; }
        public byte[] PublicKey { get; }

        public Wallet(string address, string name, byte[] privateKey, byte[] publicKey)
        {
            Address = address;
            Name = name;
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }

        public byte[] Sign(byte[] data)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(PrivateKey, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA256);
        }
    }
}