namespace BlockChainP33;

public class DisplayService
{
    public void DisplayBlockDetails(Block block)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"--- Блок #{block.Index} ---");
        Console.ResetColor();
        Console.WriteLine($"Index:        {block.Index}");
        Console.WriteLine($"Data:         {(string.IsNullOrEmpty(block.Data) ? "[Немає текстової події]" : block.Data)}");
        Console.WriteLine($"Timestamp:    {block.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"PreviousHash: {block.PreviousHash}");
        Console.WriteLine($"Hash:         {block.Hash}");
        Console.WriteLine($"Nonce: {block.Nonce} | Difficulty: {block.DifficultyAtMining} | Duration: {block.MiningDuration:F4} с | Attempts: {block.Attempts}");
        if (block.Transactions.Count > 0)
        {
            Console.WriteLine($"Transactions ({block.Transactions.Count}):");
            foreach (var tx in block.Transactions)
            {
                Console.WriteLine($"  -> [ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }
        Console.WriteLine(new string('-', 75));
    }

    public void DisplayChain(BlockChain blockChain)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"=== ВЕСЬ ЛАНЦЮГ BLOCKCHAIN (Блоків: {blockChain.Chain.Count}) ===");
        Console.ResetColor();
        foreach (var block in blockChain.Chain)
        {
            DisplayBlockDetails(block);
        }
    }

    public void SearchBlockByIndex(BlockChain blockChain, int index)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"=== Blockchain Explorer: Пошук блоку за Index = {index} ===");
        Console.ResetColor();

        var block = blockChain.FindBlockByIndex(index);
        if (block != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Знайдено блок #{block.Index}] Усі властивості блоку:");
            Console.ResetColor();
            DisplayBlockDetails(block);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Блок із Index = {index} не знайдено.");
            Console.ResetColor();
        }
    }

    public void SearchBlocksByData(BlockChain blockChain, string searchText)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"=== Blockchain Explorer: Пошук подій за текстом у Data: \"{searchText}\" ===");
        Console.ResetColor();

        var blocks = blockChain.SearchBlocksByData(searchText);
        if (blocks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Подій із текстом \"{searchText}\" у полі Data не знайдено.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Знайдено блоків: {blocks.Count}");
        Console.ResetColor();
        foreach (var block in blocks)
        {
            DisplayBlockDetails(block);
        }
    }

    public void DisplayTransactions(BlockChain blockChain, List<Models.Transactions> pendingTransactions)
    {
        Console.WriteLine("=== Очікуючі транзакції (ще не в блоці) ===");
        if (pendingTransactions.Count == 0)
        {
            Console.WriteLine("  Немає очікуючих транзакцій.");
        }
        else
        {
            for (int i = 0; i < pendingTransactions.Count; i++)
            {
                var tx = pendingTransactions[i];
                Console.WriteLine($"  [ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }

        Console.WriteLine("\n=== Підтверджені транзакції (у блоках) ===");
        bool hasConfirmed = false;
        foreach (var block in blockChain.Chain)
        {
            foreach (var tx in block.Transactions)
            {
                hasConfirmed = true;
                Console.WriteLine($"  [Блок #{block.Index} | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }

        if (!hasConfirmed)
        {
            Console.WriteLine("  У блоках ще немає транзакцій.");
        }
        Console.WriteLine(new string('-', 50));
    }

    public void SearchTransactionById(BlockChain blockChain, List<Models.Transactions> pendingTransactions, int txId)
    {
        var confirmedTx = blockChain.FindTransactionById(txId);
        if (confirmedTx != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Знайдено у блокчейні] ID: {confirmedTx.Id} | Від кого: {confirmedTx.From} | Скільки: {confirmedTx.Amount} | Кому: {confirmedTx.To} | Час: {confirmedTx.TimeStamp:HH:mm:ss}");
            Console.ResetColor();
            return;
        }

        var pendingTx = pendingTransactions.FirstOrDefault(tx => tx.Id == txId);
        if (pendingTx != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Знайдено в очікуючих] ID: {pendingTx.Id} | Від кого: {pendingTx.From} | Скільки: {pendingTx.Amount} | Кому: {pendingTx.To} | Час: {pendingTx.TimeStamp:HH:mm:ss}");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Транзакцію з ID {txId} не знайдено.");
        Console.ResetColor();
    }

    public void SearchTransactionsByName(BlockChain blockChain, List<Models.Transactions> pendingTransactions, string name)
    {
        var blocks = blockChain.SearchTransactionsByName(name);
        var pending = pendingTransactions
            .Where(tx => tx.From.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                         tx.To.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"=== Результати пошуку для '{name}' (всі транзакції) ===");
        bool found = false;

        foreach (var block in blocks)
        {
            foreach (var tx in block.Transactions.Where(tx =>
                tx.From.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                tx.To.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                found = true;
                Console.WriteLine($"  [Блок #{block.Index} | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }

        foreach (var tx in pending)
        {
            found = true;
            Console.WriteLine($"  [Очікує | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
        }

        if (!found)
        {
            Console.WriteLine($"  Транзакцій користувача '{name}' не знайдено.");
        }
        Console.WriteLine(new string('-', 50));
    }

    public void SearchTransactionsBySender(BlockChain blockChain, List<Models.Transactions> pendingTransactions, string name)
    {
        var blocks = blockChain.SearchTransactionsBySender(name);
        var pending = pendingTransactions
            .Where(tx => tx.From.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"=== Транзакції, де '{name}' є відправником ===");
        bool found = false;

        foreach (var block in blocks)
        {
            foreach (var tx in block.Transactions.Where(tx => tx.From.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                found = true;
                Console.WriteLine($"  [Блок #{block.Index} | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }

        foreach (var tx in pending)
        {
            found = true;
            Console.WriteLine($"  [Очікує | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
        }

        if (!found)
        {
            Console.WriteLine($"  Транзакцій від '{name}' не знайдено.");
        }
        Console.WriteLine(new string('-', 50));
    }

    public void SearchTransactionsByRecipient(BlockChain blockChain, List<Models.Transactions> pendingTransactions, string name)
    {
        var blocks = blockChain.SearchTransactionsByReceiver(name);
        var pending = pendingTransactions
            .Where(tx => tx.To.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"=== Транзакції, де '{name}' є отримувачем ===");
        bool found = false;

        foreach (var block in blocks)
        {
            foreach (var tx in block.Transactions.Where(tx => tx.To.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                found = true;
                Console.WriteLine($"  [Блок #{block.Index} | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
            }
        }

        foreach (var tx in pending)
        {
            found = true;
            Console.WriteLine($"  [Очікує | ID: {tx.Id}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To} | Час: {tx.TimeStamp:HH:mm:ss}");
        }

        if (!found)
        {
            Console.WriteLine($"  Транзакцій для '{name}' не знайдено.");
        }
        Console.WriteLine(new string('-', 50));
    }
}


