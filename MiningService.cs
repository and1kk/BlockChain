namespace BlockChainP33;

public class MiningService
{
    public void MineBlock(Block block, string target = "and")
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Mining] Старт майнінгу блока #{block.Index} в 2 потоки (Шукаємо: '{target}')...");
        Console.ResetColor();

        int winningNonce = 0;
        string winningHash = string.Empty;
        object lockObj = new();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2 };

        Parallel.For(0, int.MaxValue, parallelOptions, (nonce, state) =>
        {
            if (state.IsStopped) return;

            var testBlock = new Block
            {
                Index = block.Index,
                Author = block.Author,
                Data = block.Data,
                PrevHash = block.PrevHash,
                TimeStamp = block.TimeStamp,
                Nonce = nonce
            };

            var hashingService = new HashingService();
            string hash = hashingService.ComputeBlockHash(testBlock);

            if (hash.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            {
                lock (lockObj)
                {
                    if (!state.IsStopped)
                    {
                        winningNonce = nonce;
                        winningHash = hash;
                        state.Stop();
                    }
                }
            }
        });

        block.Nonce = winningNonce;
        block.Hash = winningHash;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[Mining] Блок #{block.Index} успішно змайнено! Nonce: {block.Nonce} | Hash: {block.Hash}\n");
        Console.ResetColor();
    }
}
