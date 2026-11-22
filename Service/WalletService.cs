using System.Security.Cryptography;
using WalletApp;

namespace Service
{
    public class WalletService
    {
        public Wallet CreateWallet(string name)
        {
            // Генерація пари ключів (публічного та приватного)
            using var ecdsa = ECDsa.Create();
            var privateKey = ecdsa.ExportECPrivateKey(); // Считування приватного ключа
            var publicKey = ecdsa.ExportSubjectPublicKeyInfo(); // Считування публічного ключа
            // Генерація адреси на основі публічного ключа
            var address = GetAddress(publicKey); // Скорочення адреси до 10 символів
            return new Wallet(address, name, privateKey, publicKey);
        }

        public string GetAddress(byte[] publicKey)
        {
            byte[] hash = SHA256.Create().ComputeHash(publicKey);
            return Convert.ToHexString(hash).ToLower()[..10];
        }

        public bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
        {
            using var ecdsa = ECDsa.Create(); // Створення нового екземпляру ECDsa для перевірки підпису
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _); // Считування публічного ключа
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256); // Перевірка підпису даних за допомогою публічного ключа
        }
    }
}
