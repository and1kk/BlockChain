using System.Security.Cryptography;
using System.Text;

namespace BlockChainP33;

public class HashingService
{
    public string ComputeBlockHash(Block block)
    {
        using var sha256 = SHA256.Create();
        var stringTransactions = string.Concat(block.Transactions.Select(t => t.ToRawString()));
        string rawData = $"{block.Index}{block.TimeStamp}{block.PrevHash}{stringTransactions}{block.DifficultyAtMining}{block.Nonce}";
        byte[] bytes = Encoding.UTF8.GetBytes(rawData);
        byte[] hashBytes = sha256.ComputeHash(bytes);
        // Чистый Base64 (буквы A-Z, a-z, цифры 0-9) без всяких нулей
        // return Convert.ToBase64String(hashBytes);
        return Convert.ToHexString(hashBytes);
    }
}
