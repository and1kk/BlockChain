using Models;
using Service;

namespace BlockChainP33;

public class BlockChain
{
    public List<Block> Chain { get; set; }
    public readonly decimal _targetBlockDuration = 2; // Цільова тривалість майнінгу в секундах
    public readonly int _adjustmentInterval = 3; // Кількість блоків для корекції складності
    private readonly HashingService _hashingService;
    private readonly MiningService _miningService;
    private readonly WalletService _walletService;
    private readonly TransactionService _transactionService;
    private readonly BalanceService _balanceService;
    // public string TargetPrefix { get; set; } = "and";
    private readonly decimal _miningReward = 10; // Нагорода за майнінг блоку
    public int Difficulty { get; set; } = 3;
    private void AdjustDifficulty()
    {
        if (Chain.Count % _adjustmentInterval == 0 && Chain.Count > 0)
        {
            var lastBlock = Chain[Chain.Count - 1];
            var firstBlockInInterval = Chain[Chain.Count - _adjustmentInterval];

            var actualDuration = (decimal)(lastBlock.TimeStamp - firstBlockInInterval.TimeStamp).TotalSeconds;
            var expectedDuration = _targetBlockDuration * _adjustmentInterval;

            if (actualDuration < expectedDuration / 2 && Difficulty < 7)
            {
                Difficulty++;
                Console.WriteLine($"[Difficulty Adjustment] Збільшено складність до {Difficulty}");
            }
            else if (actualDuration > expectedDuration * 2 && Difficulty > 1)
            {
                Difficulty--;
                Console.WriteLine($"[Difficulty Adjustment] Зменшено складність до {Difficulty}");
            }
        }
    }
    // public BlockChain(string targetPrefix = "and")
    public BlockChain(int difficulty = 3)
    {
        Chain = new List<Block>();
        // TargetPrefix = targetPrefix;
        Difficulty = difficulty;
        _hashingService = new HashingService();
        _miningService = new MiningService();
        _transactionService = new TransactionService();
        _walletService = new WalletService();
        _balanceService = new BalanceService();
        AddGenesisBlock();
    }

    private void AddGenesisBlock()
    {
        var genesisBlock = new Block
        {
            Index = 0,
            Transactions = new List<Transactions>(),
            PrevHash = "0",
            // TimeStamp = DateTime.Parse("2023-06-01"),
            TimeStamp = DateTime.Now,
            Nonce = 0,
            DifficultyAtMining = Difficulty
        };
        genesisBlock.Hash = _hashingService.ComputeBlockHash(genesisBlock);
        // _miningService.MineBlock(genesisBlock, TargetPrefix);
        _miningService.MineBlock(genesisBlock, Difficulty);
        Chain.Add(genesisBlock);
    }

    public Block? FindBlockByIndex(int index)
    {
        return Chain.FirstOrDefault(b => b.Index == index);
    }

    public List<Block> SearchBlocksByTransactionText(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<Block>();

        return Chain
            .Where(b => b.Transactions.Any(tx =>
                tx.From.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                tx.To.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                tx.Amount.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public List<Transactions> GetAllTransactions()
    {
        return Chain.SelectMany(b => b.Transactions).ToList();
    }

    public decimal GetBalance(string address, List<Transactions>? pendingTransactions = null)
    {
        return _balanceService.GetBalance(address, GetAllTransactions(), pendingTransactions);
    }

    public void AddBlock(List<Transactions>? transactions = null, string? minerAddress = null, bool? checkBalance = null)
    {
        transactions ??= new List<Transactions>();
        bool shouldCheckBalance = checkBalance ?? !string.IsNullOrWhiteSpace(minerAddress);
        var idsInNewBlock = new HashSet<int>();
        var runningTransactions = shouldCheckBalance ? GetAllTransactions() : null;

        foreach (var tx in transactions)
        {
            var (isValid, message) = shouldCheckBalance
                ? _transactionService.ValidateBalance(tx, runningTransactions!)
                : _transactionService.ValidateTransaction(tx);

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Невалідна транзакція ({message}): {tx.ToRawString()}");
                Console.ResetColor();
                return;
            }
            if (!idsInNewBlock.Add(tx.Id))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Дублювання ID транзакції у новому блоці: {tx.ToRawString()}");
                Console.ResetColor();
                return;
            }
            if (FindTransactionById(tx.Id) != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Транзакція з таким ID вже існує у блокчейні: {tx.ToRawString()}");
                Console.ResetColor();
                return;
            }

            runningTransactions?.Add(tx);
        }

        //надо же создать копию что бы не удалять транзакции из списка, который передали в метод
        var transactionsCopy = new List<Transactions>(transactions);
        if (!string.IsNullOrWhiteSpace(minerAddress))
        {
            transactionsCopy.Add(Transactions.CreateCoinbaseTransaction(minerAddress, _miningReward));
        }

        var lastBlock = Chain[Chain.Count - 1];
        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Transactions = transactionsCopy,
            PrevHash = lastBlock.Hash,
            TimeStamp = DateTime.Now,
            Nonce = 0,
            DifficultyAtMining = Difficulty
        };
        newBlock.Hash = _hashingService.ComputeBlockHash(newBlock);
        // _miningService.MineBlock(newBlock, TargetPrefix);
        _miningService.MineBlock(newBlock, Difficulty);
        Chain.Add(newBlock);
        AdjustDifficulty();
    }

    public void SetMiningMode(MiningMode mode)
    {
        _miningService.CurrentMode = mode;
    }

    public MiningMode GetMiningMode() => _miningService.CurrentMode;

    public async Task<bool> AddBlockAsync(List<Transactions>? transactions = null, MiningMode? mode = null, CancellationToken cancellationToken = default, string? minerAddress = null, bool? checkBalance = null)
    {
        transactions ??= new List<Transactions>();
        bool shouldCheckBalance = checkBalance ?? !string.IsNullOrWhiteSpace(minerAddress);
        var idsInNewBlock = new HashSet<int>();
        var runningTransactions = shouldCheckBalance ? GetAllTransactions() : null;

        foreach (var tx in transactions)
        {
            var (isValid, message) = shouldCheckBalance
                ? _transactionService.ValidateBalance(tx, runningTransactions!)
                : _transactionService.ValidateTransaction(tx);

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Невалідна транзакція ({message}): {tx.ToRawString()}");
                Console.ResetColor();
                return false;
            }
            if (!idsInNewBlock.Add(tx.Id))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Дублювання ID транзакції у новому блоці: {tx.ToRawString()}");
                Console.ResetColor();
                return false;
            }
            if (FindTransactionById(tx.Id) != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] Транзакція з таким ID вже існує у блокчейні: {tx.ToRawString()}");
                Console.ResetColor();
                return false;
            }

            runningTransactions?.Add(tx);
        }

        var transactionsCopy = new List<Transactions>(transactions);
        if (!string.IsNullOrWhiteSpace(minerAddress))
        {
            transactionsCopy.Add(Transactions.CreateCoinbaseTransaction(minerAddress, _miningReward));
        }

        var lastBlock = Chain[Chain.Count - 1];
        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Transactions = transactionsCopy,
            PrevHash = lastBlock.Hash,
            TimeStamp = DateTime.Now,
            Nonce = 0,
            DifficultyAtMining = Difficulty
        };

        await _miningService.MineBlockAsync(newBlock, Difficulty, mode, cancellationToken);
        Chain.Add(newBlock);
        AdjustDifficulty();
        return true;
    }

    public async Task CompareAllMiningModesAsync(int difficulty = 5)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n=== ПОРІВНЯННЯ 4 РЕЖИМІВ МАЙНІНГУ (Однакова складність: Difficulty = {difficulty}) ===");
        Console.ResetColor();

        var fixedTime = new DateTime(2026, 11, 15, 19, 0, 0);
        var sampleTx = new Transactions("Alex", "Bob", 250m) { Id = 999, TimeStamp = fixedTime };

        Block CreateIdenticalBlock() => new Block
        {
            Index = Chain.Count,
            Transactions = new List<Transactions> { sampleTx },
            PrevHash = "00000BENCHMARK_BLOCK_2026_ASYNC_TEST",
            TimeStamp = fixedTime,
            Nonce = 0,
            DifficultyAtMining = difficulty
        };

        // 1. CPU 1-Thread
        var bCpu1 = CreateIdenticalBlock();
        await _miningService.MineBlockAsync(bCpu1, difficulty, MiningMode.CpuSingleThread);

        // 2. CPU Multi-Thread
        var bCpuMulti = CreateIdenticalBlock();
        await _miningService.MineBlockAsync(bCpuMulti, difficulty, MiningMode.CpuMultiThread);

        // 3. GPU 1-Stream
        var bGpu1 = CreateIdenticalBlock();
        await _miningService.MineBlockAsync(bGpu1, difficulty, MiningMode.GpuSingleThread);

        // 4. GPU Multi-Stream
        var bGpuMulti = CreateIdenticalBlock();
        await _miningService.MineBlockAsync(bGpuMulti, difficulty, MiningMode.GpuMultiThread);

        decimal tCpu1 = Math.Max(bCpu1.MiningDuration, 0.0001m);
        decimal tCpuM = Math.Max(bCpuMulti.MiningDuration, 0.0001m);
        decimal tGpu1 = Math.Max(bGpu1.MiningDuration, 0.0001m);
        decimal tGpuM = Math.Max(bGpuMulti.MiningDuration, 0.0001m);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== ПІДСУМКОВА ТАБЛИЦЯ ПОРІВНЯННЯ (Difficulty = " + difficulty + ") ===");
        Console.ResetColor();
        Console.WriteLine($"1. CPU (1 потік, звичайний):         {bCpu1.MiningDuration:F4} с | Nonce: {bCpu1.Nonce} | Базовий час (1.00x)");
        Console.WriteLine($"2. CPU ({Environment.ProcessorCount} потоків, багатопотоковий): {bCpuMulti.MiningDuration:F4} с | Nonce: {bCpuMulti.Nonce} | Прискорення: {Math.Round(tCpu1 / tCpuM, 2)}x швидше за CPU 1-потік");
        Console.WriteLine($"3. GPU (1 потік подачі батчів):      {bGpu1.MiningDuration:F4} с | Nonce: {bGpu1.Nonce} | Прискорення: {Math.Round(tCpu1 / tGpu1, 2)}x швидше за CPU 1-потік");
        Console.WriteLine($"4. GPU (4 паралельні потоки/стріми): {bGpuMulti.MiningDuration:F4} с | Nonce: {bGpuMulti.Nonce} | Прискорення: {Math.Round(tCpu1 / tGpuM, 2)}x швидше за CPU 1-потік ({Math.Round(tGpu1 / tGpuM, 2)}x швидше за GPU 1-потік)");
        Console.WriteLine(new string('-', 85));
    }

    public void PrintDifficultyHistory()
    {
        Console.WriteLine("\n=== Історія складності (Difficulty History) ===");
        foreach (var block in Chain)
        {
            Console.WriteLine(
                $"Блок #{block.Index} | Difficulty: {block.DifficultyAtMining} | " +
                $"Attempts: {block.Attempts} | MiningDuration: {block.MiningDuration} с");
        }
        Console.WriteLine(new string('-', 60));
    }
    public Transactions? FindTransactionById(int transactionId)
    {
        foreach (var block in Chain)
        {
            var transaction = block.Transactions.FirstOrDefault(tx => tx.Id == transactionId);
            if (transaction != null)
            {
                return transaction;
            }
        }
        return null; // Транзакція не знайдена
    }
    //Пошук всіх блоків, де зустрічається ім'я користувача (відправник або отримувач)
    public List<Block> SearchTransactionsByName(string name)
    {
        var matchingBlocks = new List<Block>();

        foreach (var block in Chain)
        {
            if (block.Transactions.Any(tx => tx.From.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                             tx.To.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                matchingBlocks.Add(block);
            }
        }

        return matchingBlocks;
    }

public List<Block> SearchTransactionsBySender(string name)
    {
        var matchingBlocks = new List<Block>();

        foreach (var block in Chain)
        {
            if (block.Transactions.Any(tx => tx.From.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                matchingBlocks.Add(block);
            }
        }

        return matchingBlocks;
    }

    public List<Block> SearchTransactionsByReceiver(string name)
    {
        var matchingBlocks = new List<Block>();

        foreach (var block in Chain)
        {
            if (block.Transactions.Any(tx => tx.To.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                matchingBlocks.Add(block);
            }
        }

        return matchingBlocks;
    }

    public bool IsValid(out int firstInvalidBlockIndex, out string errorReason)
    {
        for (int i = 0; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];

            // 1. Перевірка правильності Hash поточного блоку
            string recalculatedHash = _hashingService.ComputeBlockHash(currentBlock);
            if (currentBlock.Hash != recalculatedHash)
            {
                firstInvalidBlockIndex = currentBlock.Index;
                errorReason = $"Неправильний Hash у блоці #{currentBlock.Index} (збережений: {currentBlock.Hash}, перерахований: {recalculatedHash})";
                return false;
            }

            // 2. Перевірка зв'язку через PreviousHash (для всіх блоків після Genesis)
            if (i > 0)
            {
                var previousBlock = Chain[i - 1];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    firstInvalidBlockIndex = currentBlock.Index;
                    errorReason = $"Порушений зв'язок PreviousHash у блоці #{currentBlock.Index} (вказує на: {currentBlock.PreviousHash}, а Hash блоку #{previousBlock.Index}: {previousBlock.Hash})";
                    return false;
                }
            }
        }

        firstInvalidBlockIndex = -1;
        errorReason = string.Empty;
        return true;
    }

    public bool IsValid()
    {
        bool valid = IsValid(out int invalidIndex, out string reason);
        if (valid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[IsValid] Blockchain валідний! Усі Hash та зв'язки PreviousHash коректні.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[IsValid] УВАГА! Blockchain пошкоджений! Перший неправильний блок: #{invalidIndex}");
            Console.WriteLine($"[IsValid] Причина: {reason}");
            Console.ResetColor();
        }
        return valid;
    }

    public bool AuditBlockchain()
    {
        var issues = new List<(int BlockIndex, string Reason)>();
        var seenTransactionIds = new Dictionary<int, int>(); // txId -> first block index
        var userOperationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int totalTransactions = 0;
        decimal totalTransferAmount = 0m;

        for (int i = 0; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];

            // 1. Перевірка Hash кожного блоку
            string recalculatedHash = _hashingService.ComputeBlockHash(currentBlock);
            if (currentBlock.Hash != recalculatedHash)
            {
                issues.Add((
                    currentBlock.Index,
                    $"Порушення цілісності Hash (збережений: {currentBlock.Hash}, перерахований: {recalculatedHash})"
                ));
            }

            // 2. Перевірка правильності PreviousHash (для блоків після Genesis)
            if (i > 0)
            {
                var previousBlock = Chain[i - 1];
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    issues.Add((
                        currentBlock.Index,
                        $"Неправильний PreviousHash (вказує на: {currentBlock.PreviousHash}, очікувався Hash блоку #{previousBlock.Index}: {previousBlock.Hash})"
                    ));
                }
            }

            // 3. Перевірка валідності усіх транзакцій та відсутності дублювання Transaction Id
            foreach (var tx in currentBlock.Transactions)
            {
                totalTransactions++;
                totalTransferAmount += tx.Amount;

                if (!string.IsNullOrWhiteSpace(tx.From))
                {
                    userOperationCounts[tx.From] = userOperationCounts.GetValueOrDefault(tx.From) + 1;
                }
                if (!string.IsNullOrWhiteSpace(tx.To))
                {
                    userOperationCounts[tx.To] = userOperationCounts.GetValueOrDefault(tx.To) + 1;
                }

                var (txValid, txMessage) = _transactionService.ValidateTransaction(tx);
                if (!txValid)
                {
                    issues.Add((
                        currentBlock.Index,
                        $"Невалідна транзакція ID #{tx.Id} ({tx.From} -> {tx.To}, Amount: {tx.Amount}): {txMessage}"
                    ));
                }

                if (seenTransactionIds.TryGetValue(tx.Id, out int firstBlockIndex))
                {
                    issues.Add((
                        currentBlock.Index,
                        $"Дублювання Transaction Id #{tx.Id} (вже використовувався у блоці #{firstBlockIndex})"
                    ));
                }
                else
                {
                    seenTransactionIds[tx.Id] = currentBlock.Index;
                }
            }
        }

        string topUserInfo = "Немає операцій";
        if (userOperationCounts.Count > 0)
        {
            var topUser = userOperationCounts.OrderByDescending(kv => kv.Value).First();
            topUserInfo = $"{topUser.Key} ({topUser.Value} операцій)";
        }

        bool isValid = issues.Count == 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("===========================================================================");
        Console.WriteLine("                         ЗВІТ BLOCKCHAIN AUDIT");
        Console.WriteLine("===========================================================================");
        Console.ResetColor();
        Console.WriteLine($"  Кількість блоків:                      {Chain.Count}");
        Console.WriteLine($"  Загальна кількість транзакцій:         {totalTransactions}");
        Console.WriteLine($"  Загальна сума переказів:               {totalTransferAmount:F2}");
        Console.WriteLine($"  Найактивніший користувач:              {topUserInfo}");

        if (isValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Результат перевірки Blockchain:        ВАЛІДНИЙ (порушень не виявлено)");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Результат перевірки Blockchain:        ПОШКОДЖЕНИЙ (виявлено порушень: {issues.Count})");
            Console.WriteLine("\n  Деталі виявлених проблем:");
            foreach (var (blockIndex, reason) in issues)
            {
                Console.WriteLine($"   [!] Блок #{blockIndex}: {reason}");
            }
            Console.ResetColor();
        }

        Console.WriteLine("===========================================================================");
        return isValid;
    }

    public bool IsChainValid()
    {
        for (int i = 1; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var previousBlock = Chain[i - 1];

            // 1. Перевірка хешу поточного блоку
            if (currentBlock.Hash != _hashingService.ComputeBlockHash(currentBlock))
            {
                return false;
            }

            // 2. Перевірка хешу попереднього блоку
            if (currentBlock.PrevHash != previousBlock.Hash)
            {
                return false;
            }

            // 3. Перевірка префіксу майнінгу
            // if (!currentBlock.Hash.StartsWith(TargetPrefix, StringComparison.OrdinalIgnoreCase))
            int blockDifficulty = currentBlock.DifficultyAtMining > 0 ? currentBlock.DifficultyAtMining : Difficulty;
            string target = new string('0', blockDifficulty);
            if (!currentBlock.Hash.StartsWith(target))
            {
                return false;
            }
        }

        return true;
    }

    public Block? FindFirstInvalidBlock()
    {
        for (int i = 0; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];

            // 1. Перевірка хешу поточного блоку
            string recalculatedHash = _hashingService.ComputeBlockHash(currentBlock);
            if (currentBlock.Hash != recalculatedHash)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Порушення!] Перший пошкоджений блок: #{currentBlock.Index}");
                Console.WriteLine($"Причина: Неправильний Hash (збережений: {currentBlock.Hash}, перерахований: {recalculatedHash})");
                Console.ResetColor();
                return currentBlock;
            }

            // 2. Перевірка PreviousHash (для блоків починаючи з #1)
            if (i > 0)
            {
                var previousBlock = Chain[i - 1];
                if (currentBlock.PrevHash != previousBlock.Hash)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Порушення!] Перший пошкоджений блок: #{currentBlock.Index}");
                    Console.WriteLine($"Причина: Порушений PrevHash (вказує на: {currentBlock.PrevHash}, а хеш блоку #{previousBlock.Index}: {previousBlock.Hash})");
                    Console.ResetColor();
                    return currentBlock;
                }
            }

            // 3. Перевірка Proof of Work за DifficultyAtMining (а не за поточною Difficulty!)
            string target = new string('0', currentBlock.DifficultyAtMining);
            if (!currentBlock.Hash.StartsWith(target))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Порушення!] Перший пошкоджений блок: #{currentBlock.Index}");
                Console.WriteLine($"Причина: Невідповідність Proof of Work (хеш '{currentBlock.Hash}' не починається з '{target}', DifficultyAtMining = {currentBlock.DifficultyAtMining})");
                Console.ResetColor();
                return currentBlock;
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[OK] Всі блоки валідні, порушень не знайдено.");
        Console.ResetColor();
        return null;
    }

    public void RecalculateBlockHashManually(int index)
    {
        if (index >= 0 && index < Chain.Count)
        {
            Chain[index].Hash = _hashingService.ComputeBlockHash(Chain[index]);
        }
    }
}
