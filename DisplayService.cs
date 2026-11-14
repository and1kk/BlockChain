namespace BlockChainP33;

public class DisplayService
{
    public void DisplayChain(BlockChain blockChain)
    {
        foreach (var block in blockChain.Chain)
        {
            Console.WriteLine($"Index: {block.Index}");
            Console.WriteLine($"Author: {block.Author}");
            Console.WriteLine($"Hash: {block.Hash}");
            Console.WriteLine($"Data: {block.Data}");
            Console.WriteLine($"PrevHash: {block.PrevHash}");
            Console.WriteLine($"TimeStamp: {block.TimeStamp}");
            Console.WriteLine($"Nonce: {block.Nonce}");
            Console.WriteLine(new string('-', 30));
        }
    }
}
