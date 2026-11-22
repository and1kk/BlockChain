using BlockChainP33;
using Models;
using Service;
using WalletApp;

var blockChainService = new BlockChain(4);
var displayService = new DisplayService();
var transactionService = new TransactionService();
var walletService = new WalletService();

var bobWallet = walletService.CreateWallet("Bob");
var aliceWallet = walletService.CreateWallet("Alice");
var charlieWallet = walletService.CreateWallet("Charlie");

var wallets = new Dictionary<string, Wallet>(StringComparer.OrdinalIgnoreCase)
{
    ["Bob"] = bobWallet,
    ["Alice"] = aliceWallet,
    ["Charlie"] = charlieWallet
};

Wallet GetOrCreateWallet(string name)
{
    if (!wallets.TryGetValue(name, out var wallet))
    {
        wallet = walletService.CreateWallet(name);
        wallets[name] = wallet;
    }
    return wallet;
}

var tempListTransactions = new List<Transactions>
{
    transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 10, aliceWallet)
};

(string From, string To, decimal Amount)[] sampleTransfers =
{
    ("Alex", "Bob", 100m),
    ("Bob", "Charlie", 50m),
    ("Charlie", "Dave", 25m),
    ("Dave", "Eve", 10m),
    ("Eve", "Frank", 5m),
    ("Frank", "Alex", 15m)
};
int txCounter = 0;

void PrintTransactionInfo(Transactions tx, string label = "Транзакція")
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[{label}] Від кого: {tx.From} | Скільки: {tx.Amount} | Кому: {tx.To}");
    Console.ResetColor();
}

Task? backgroundMiningTask = null;

bool running = true;
while (running)
{
    string bgStatus = backgroundMiningTask != null && !backgroundMiningTask.IsCompleted
        ? "Виконується у фоні..."
        : "Немає активних задач";

    Console.WriteLine($"\n=== Меню Блокчейну [Режим: {blockChainService.GetMiningMode()} | Фоновий майнінг: {bgStatus}] ===");
    Console.WriteLine("1. Додати 1 блок (автоматичні дані)");
    Console.WriteLine("2. Додати 8 блоків підряд (автоматичні дані)");
    Console.WriteLine("3. Показати весь ланцюжок");
    Console.WriteLine("4. Перевірити валідність та знайти пошкоджений блок");
    Console.WriteLine("5. Пошкодити дані у блоці #2");
    Console.WriteLine("6. Показати історію складності");
    Console.WriteLine("7. Спробувати приховати підробку блоку #2 (перерахувати Hash)");
    Console.WriteLine("8. Замайнити блок");
    Console.WriteLine("9. Створити транзакцію");
    Console.WriteLine("10. Показати транзакції");
    Console.WriteLine("11. Пошук за id транзакції");
    Console.WriteLine("12. Пошук за ім'ям ");
    Console.WriteLine("13. Тест перевірки транзакцій перед майнінгом");
    Console.WriteLine("14. Порівняти 4 режими майнінгу (CPU 1-потік / CPU потоки / GPU / GPU потоки)");
    Console.WriteLine("15. Асинхронний майнінг у фоні (програма залишається доступною)");
    Console.WriteLine("16. Змінити режим майнінгу (CPU / CPU Multi / GPU / GPU Multi)");
    Console.WriteLine("17. [ДЗ #3] Власний Blockchain (5 блоків із транзакціями + Explorer + IsValid)");
    Console.WriteLine("18. [ДЗ #3] Blockchain Explorer (пошук блоку за Index або текстом у транзакціях)");
    Console.WriteLine("19. [ДЗ #3] Перевірка безпеки IsValid() та тест підробки блоку #2");
    Console.WriteLine("20. [ДЗ #4] Blockchain Audit (повна перевірка + звіт + тест зміни Amount)");
    Console.WriteLine("21. [ДЗ #4] Запустити AuditBlockchain() для поточного ланцюга");
    Console.WriteLine("22. [Гаманці] Додати підписану транзакцію (Alice -> Bob, 10) та показати гаманці");
    Console.WriteLine("23. [ДЗ #5] Цифровий підпис ECDSA: повний приклад + підробка суми + підпис чужим гаманцем");
    Console.WriteLine("0. Вихід");
    Console.Write("Оберіть дію: ");

    var choice = Console.ReadLine()?.Trim();
    Console.WriteLine();

    switch (choice)
    {
        case "1":
        {
            var sample = sampleTransfers[txCounter % sampleTransfers.Length];
            txCounter++;
            var fromWallet = GetOrCreateWallet(sample.From);
            var toWallet = GetOrCreateWallet(sample.To);
            var tx = transactionService.CreateTransaction(fromWallet.Address, toWallet.Address, sample.Amount, fromWallet);
            PrintTransactionInfo(tx, "Авто-транзакція");
            blockChainService.AddBlock(new List<Transactions> { tx });
            break;
        }
        case "2":
        {
            for (int i = 0; i < 8; i++)
            {
                var sample = sampleTransfers[txCounter % sampleTransfers.Length];
                txCounter++;
                var fromWallet = GetOrCreateWallet(sample.From);
                var toWallet = GetOrCreateWallet(sample.To);
                var tx = transactionService.CreateTransaction(fromWallet.Address, toWallet.Address, sample.Amount, fromWallet);
                PrintTransactionInfo(tx, $"Авто-транзакція #{i + 1}");
                blockChainService.AddBlock(new List<Transactions> { tx });
            }
            break;
        }
        case "3":
        {
            displayService.DisplayChain(blockChainService);
            break;
        }
        case "4":
        {
            var isValid = blockChainService.IsChainValid();
            Console.WriteLine(isValid ? "Blockchain is valid." : "Blockchain is not valid.");
            blockChainService.FindFirstInvalidBlock();
            break;
        }
        case "5":
        {
            if (blockChainService.Chain.Count > 2)
            {
                if (blockChainService.Chain[2].Transactions.Count > 0)
                {
                    var targetTx = blockChainService.Chain[2].Transactions[0];
                    targetTx.Amount = 1000000m;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Злом] Транзакцію у блоці #2 змінено: {targetTx.From} -> {targetTx.To} | Сума: {targetTx.Amount}");
                    Console.ResetColor();
                }
                else
                {
                    var fakeTx = new Transactions("Hacker", "Hacker", 1000000m);
                    blockChainService.Chain[2].Transactions.Add(fakeTx);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Злом] У блок #2 додано підроблену транзакцію: {fakeTx.From} -> {fakeTx.To} | Сума: {fakeTx.Amount}");
                    Console.ResetColor();
                }
                blockChainService.FindFirstInvalidBlock();
            }
            else
            {
                Console.WriteLine("Спочатку додайте хоча б 2 блоки.");
            }
            break;
        }
        case "6":
        {
            blockChainService.PrintDifficultyHistory();
            break;
        }
        case "7":
        {
            if (blockChainService.Chain.Count > 2)
            {
                blockChainService.RecalculateBlockHashManually(2);
                Console.WriteLine($"Хеш блоку #2 вручну перераховано без майнінгу: {blockChainService.Chain[2].Hash}");
                blockChainService.FindFirstInvalidBlock();
            }
            else
            {
                Console.WriteLine("Спочатку додайте хоча б 2 блоки.");
            }
            break;
        }
        case "8":
        {
            if (tempListTransactions.Count > 0)
            {
                Console.WriteLine($"Майнінг блоку ({tempListTransactions.Count} транзакцій):");
                foreach (var tx in tempListTransactions)
                {
                    PrintTransactionInfo(tx);
                }
            }
            else
            {
                Console.WriteLine("Майнінг блоку без транзакцій...");
            }

            blockChainService.AddBlock(tempListTransactions);
            tempListTransactions.Clear();
            break;
        }
        case "9":
        {
            Console.Write("Від кого (ім'я гаманця, напр. Alice / Bob / Charlie): ");
            string fromName = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Кому (ім'я гаманця, напр. Bob / Charlie / Alice): ");
            string toName = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Скільки: ");
            string? amountInput = Console.ReadLine();

            if (!decimal.TryParse(amountInput, out decimal amount))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Невірний формат суми.");
                Console.ResetColor();
                break;
            }

            try
            {
                var fromWallet = GetOrCreateWallet(fromName);
                var toWallet = GetOrCreateWallet(toName);
                var newTx = transactionService.CreateTransaction(fromWallet.Address, toWallet.Address, amount, fromWallet);
                tempListTransactions.Add(newTx);
                PrintTransactionInfo(newTx, "Нова підписана транзакція");
            }
            catch (ArgumentException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] {ex.Message}");
                Console.ResetColor();
            }
            break;
        }
        case "10":
        {
            displayService.DisplayTransactions(blockChainService, tempListTransactions);
            break;
        }
        case "11":
        {
            Console.Write("Введіть ID транзакції для пошуку: ");
            string? idInput = Console.ReadLine();
            if (int.TryParse(idInput, out int txId))
            {
                displayService.SearchTransactionById(blockChainService, tempListTransactions, txId);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Невірний формат ID.");
                Console.ResetColor();
            }
            break;
        }
        case "12":
        {
            Console.Write("Введіть ім'я для пошуку транзакцій: ");
            string? nameInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(nameInput))
            {
                Console.WriteLine($"1. Показати всі транзакції: {nameInput}");
                Console.WriteLine($"2. Показати всі транзакції де {nameInput} є відправником");
                Console.WriteLine($"3. Показати всі транзакції де {nameInput} є отримувачем");
                Console.Write("Оберіть опцію (1, 2 або 3): ");
                string? optionInput = Console.ReadLine()?.Trim();

                switch (optionInput)
                {
                    case "1":
                        displayService.SearchTransactionsByName(blockChainService, tempListTransactions, nameInput);
                        break;
                    case "2":
                        displayService.SearchTransactionsBySender(blockChainService, tempListTransactions, nameInput);
                        break;
                    case "3":
                        displayService.SearchTransactionsByRecipient(blockChainService, tempListTransactions, nameInput);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Помилка] Невірна опція.");
                        Console.ResetColor();
                        break;
                }
            }
           
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Ім'я не може бути порожнім.");
                Console.ResetColor();
            }
            break;
        }
        case "13":
        {
            Console.WriteLine($"Кількість блоків ДО тестів: {blockChainService.Chain.Count}\n");

            // Тест 1: 5 транзакцій, де одна (4-та) має неправильну суму (-50)
            Console.WriteLine("--- Тест 1: 5 транзакцій, одна з неправильною сумою ---");
            var listWithInvalid = new List<Transactions>
            {
                transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 10, aliceWallet),
                transactionService.CreateTransaction(bobWallet.Address, charlieWallet.Address, 20, bobWallet),
                transactionService.CreateTransaction(charlieWallet.Address, aliceWallet.Address, 30, charlieWallet),
                new Transactions(aliceWallet.Address, bobWallet.Address, -50, aliceWallet.PublicKey), // Невалідна сума!
                transactionService.CreateTransaction(bobWallet.Address, aliceWallet.Address, 40, bobWallet)
            };
            blockChainService.AddBlock(listWithInvalid);
            Console.WriteLine($"Кількість блоків після Тесту 1: {blockChainService.Chain.Count}\n");

            // Тест 2: Дублікат ID всередині одного нового блоку
            Console.WriteLine("--- Тест 2: Повторення ID всередині нового блоку ---");
            var tx1 = transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 10, aliceWallet);
            var tx2 = transactionService.CreateTransaction(bobWallet.Address, charlieWallet.Address, 20, bobWallet);
            tx2.Id = tx1.Id; // Такий самий ID!
            blockChainService.AddBlock(new List<Transactions> { tx1, tx2 });
            Console.WriteLine($"Кількість блоків після Тесту 2: {blockChainService.Chain.Count}\n");

            // Тест 3: Транзакція з таким ID вже існує в Blockchain
            Console.WriteLine("--- Тест 3: Транзакція вже існує в Blockchain ---");
            var existingTx = transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 100, aliceWallet);
            blockChainService.AddBlock(new List<Transactions> { existingTx }); // Спочатку додаємо легально
            Console.WriteLine($"Кількість блоків після успішного додавання: {blockChainService.Chain.Count}");

            // Тепер пробуємо додати новий блок з цією ж транзакцією
            blockChainService.AddBlock(new List<Transactions> { existingTx });
            Console.WriteLine($"Кількість блоків після спроби повторного додавання: {blockChainService.Chain.Count}");
            break;
        }
        case "14":
        {
            await blockChainService.CompareAllMiningModesAsync(5);
            break;
        }
        case "15":
        {
            if (backgroundMiningTask != null && !backgroundMiningTask.IsCompleted)
            {
                Console.WriteLine("[Async Mining] Фоновий майнінг вже виконується! Ви можете користуватися меню далі.");
                break;
            }

            var batchToMine = new List<Transactions>(tempListTransactions);
            tempListTransactions.Clear();

            int oldDiff = blockChainService.Difficulty;
            blockChainService.Difficulty = 5;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("[Async Mining] Фоновий асинхронний майнінг запущено! Меню залишається доступним під час пошуку хешу.");
            Console.ResetColor();

            backgroundMiningTask = Task.Run(async () =>
            {
                await blockChainService.AddBlockAsync(batchToMine, MiningMode.CpuMultiThread);
                blockChainService.Difficulty = oldDiff;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[Async Mining] Фоновий майнінг завершено! Додано блок #{blockChainService.Chain.Count - 1}.");
                Console.ResetColor();
            });
            break;
        }
        case "16":
        {
            Console.WriteLine("Оберіть режим майнінгу:");
            Console.WriteLine("1. Просто через процесор (CPU 1 потік)");
            Console.WriteLine("2. Через процесор з багатопоточністю (CPU 16 потоків)");
            Console.WriteLine("3. Через відеокарту (GPU 1 потік подачі)");
            Console.WriteLine("4. Через відеокарту з потоками (GPU 4 паралельні потоки/стріми)");
            Console.Write("Ваш вибір: ");
            string? mInput = Console.ReadLine()?.Trim();
            if (int.TryParse(mInput, out int mVal) && mVal >= 1 && mVal <= 4)
            {
                blockChainService.SetMiningMode((MiningMode)mVal);
                Console.WriteLine($"Режим майнінгу змінено на: {blockChainService.GetMiningMode()}");
            }
            else
            {
                Console.WriteLine("Невірний вибір режиму.");
            }
            break;
        }
        case "17":
        {
            var demoChain = new BlockChain(3);
            demoChain.SetMiningMode(blockChainService.GetMiningMode());

            var systemWallet = GetOrCreateWallet("System");
            var alexWallet = GetOrCreateWallet("Alex");
            var shopWallet = GetOrCreateWallet("Shop");
            var mariaWallet = GetOrCreateWallet("Maria");

            var demoTransactions = new[]
            {
                transactionService.CreateTransaction(systemWallet.Address, alexWallet.Address, 5000m, systemWallet),
                transactionService.CreateTransaction(alexWallet.Address, shopWallet.Address, 3200m, alexWallet),
                transactionService.CreateTransaction(alexWallet.Address, mariaWallet.Address, 1200m, alexWallet),
                transactionService.CreateTransaction(mariaWallet.Address, bobWallet.Address, 450m, mariaWallet),
                transactionService.CreateTransaction(bobWallet.Address, alexWallet.Address, 150m, bobWallet)
            };

            foreach (var tx in demoTransactions)
            {
                demoChain.AddBlock(new List<Transactions> { tx });
            }

            if (!Console.IsOutputRedirected) Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 1: СТВОРЕННЯ ЛАНЦЮГА (5 БЛОКІВ ІЗ ТРАНЗАКЦІЯМИ)");
            Console.WriteLine("===========================================================================");
            Console.ResetColor();
            displayService.DisplayChain(demoChain);

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для переходу до Рівня 2 (Blockchain Explorer)...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 2: BLOCKCHAIN EXPLORER (ПОШУК ЗА INDEX ТА ТРАНЗАКЦІЯМИ)");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();
            displayService.SearchBlockByIndex(demoChain, 3);
            Console.WriteLine();
            displayService.SearchBlocksByTransactionText(demoChain, mariaWallet.Address);

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для переходу до Рівня 3 (Перевірка безпеки IsValid)...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 3: ПЕРЕВІРКА БЕЗПЕКИ IsValid() ТА ПІДРОБКА БЛОКУ #2");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1) Перевірка цілісності оригінального ланцюга (IsValid()):");
            Console.ResetColor();
            demoChain.IsValid();

            var targetTx = demoChain.Chain[2].Transactions[0];
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n2) Втручання у дані: змінюємо транзакцію у блоці #2...");
            Console.WriteLine($"   Було:  {targetTx.From} -> {targetTx.To} ({targetTx.Amount})");
            targetTx.Amount = 999999m;
            Console.WriteLine($"   Стало: {targetTx.From} -> {targetTx.To} ({targetTx.Amount})");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n3) Повторна перевірка цілісності після втручання (IsValid()):");
            Console.ResetColor();
            demoChain.IsValid();
            break;
        }
        case "18":
        {
            Console.WriteLine("=== Blockchain Explorer ===");
            Console.WriteLine("1. Пошук блоку за Index");
            Console.WriteLine("2. Пошук блоків за текстом у транзакціях");
            Console.Write("Оберіть режим пошуку (1 або 2): ");
            string? explorerChoice = Console.ReadLine()?.Trim();

            if (explorerChoice == "1")
            {
                Console.Write("Введіть Index блоку: ");
                if (int.TryParse(Console.ReadLine()?.Trim(), out int idx))
                {
                    displayService.SearchBlockByIndex(blockChainService, idx);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Помилка] Невірний формат Index.");
                    Console.ResetColor();
                }
            }
            else if (explorerChoice == "2")
            {
                Console.Write("Введіть текст для пошуку в транзакціях: ");
                string? query = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    displayService.SearchBlocksByTransactionText(blockChainService, query);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Помилка] Текст пошуку не може бути порожнім.");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Невірний вибір.");
                Console.ResetColor();
            }
            break;
        }
        case "19":
        {
            Console.WriteLine("Перевірка поточного ланцюга методом IsValid():");
            blockChainService.IsValid();

            if (blockChainService.Chain.Count > 2)
            {
                Console.Write("Змінити транзакцію у блоці #2 для перевірки безпеки? (y/n): ");
                string? ans = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (ans == "y" || ans == "yes" || ans == "так" || ans == "д")
                {
                    if (blockChainService.Chain[2].Transactions.Count > 0)
                    {
                        var tx = blockChainService.Chain[2].Transactions[0];
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Злом] Стара сума у блоці #2: {tx.Amount}");
                        tx.Amount = 999999m;
                        Console.WriteLine($"[Злом] Нова сума у блоці #2:  {tx.Amount}");
                        Console.ResetColor();
                    }
                    else
                    {
                        blockChainService.Chain[2].Transactions.Add(new Transactions("Hacker", "Hacker", 999999m));
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Злом] У блок #2 додано підроблену транзакцію.");
                        Console.ResetColor();
                    }
                    blockChainService.IsValid();
                }
            }
            else
            {
                Console.WriteLine("Для тесту зміни 2-го блоку додайте хоча б 2 блоки.");
            }
            break;
        }
        case "20":
        {
            var auditChain = new BlockChain(3);
            auditChain.SetMiningMode(blockChainService.GetMiningMode());

            var alexWallet = GetOrCreateWallet("Alex");
            var shopWallet = GetOrCreateWallet("Shop");
            var mariaWallet = GetOrCreateWallet("Maria");
            var daveWallet = GetOrCreateWallet("Dave");

            var batch1 = new List<Transactions>
            {
                transactionService.CreateTransaction(alexWallet.Address, bobWallet.Address, 500m, alexWallet),
                transactionService.CreateTransaction(bobWallet.Address, mariaWallet.Address, 200m, bobWallet)
            };
            var batch2 = new List<Transactions>
            {
                transactionService.CreateTransaction(alexWallet.Address, shopWallet.Address, 1250m, alexWallet),
                transactionService.CreateTransaction(mariaWallet.Address, alexWallet.Address, 300m, mariaWallet)
            };
            var batch3 = new List<Transactions>
            {
                transactionService.CreateTransaction(alexWallet.Address, charlieWallet.Address, 450m, alexWallet)
            };
            var batch4 = new List<Transactions>
            {
                transactionService.CreateTransaction(charlieWallet.Address, daveWallet.Address, 150m, charlieWallet),
                transactionService.CreateTransaction(alexWallet.Address, daveWallet.Address, 100m, alexWallet)
            };

            auditChain.AddBlock(batch1);
            auditChain.AddBlock(batch2);
            auditChain.AddBlock(batch3);
            auditChain.AddBlock(batch4);

            if (!Console.IsOutputRedirected) Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #4 — BLOCKCHAIN AUDIT: ПЕРЕВІРКА ВАЛІДНОГО ЛАНЦЮГА ТА ЗВІТ");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            displayService.DisplayChain(auditChain);
            Console.WriteLine();
            auditChain.AuditBlockchain();

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для тесту підробки Amount у блоці #2 та повторного AuditBlockchain()...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #4 — BLOCKCHAIN AUDIT: ТЕСТ ЗМІНИ AMOUNT В УЖЕ СТВОРЕНОМУ БЛОЦІ #2");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            var tamperedTx = auditChain.Chain[2].Transactions[0];
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Втручання] Змінюємо Amount транзакції ID #{tamperedTx.Id} у блоці #2:");
            Console.WriteLine($"  Було:  {tamperedTx.From} -> {tamperedTx.To} | Amount = {tamperedTx.Amount:F2}");
            tamperedTx.Amount = 999999m;
            Console.WriteLine($"  Стало: {tamperedTx.From} -> {tamperedTx.To} | Amount = {tamperedTx.Amount:F2}\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Запуск повторного AuditBlockchain() після підробки Amount:");
            Console.ResetColor();
            auditChain.AuditBlockchain();

            Console.WriteLine("\nДеталі пошкодженого блоку #2:");
            displayService.DisplayBlockDetails(auditChain.Chain[2]);
            break;
        }
        case "21":
        {
            blockChainService.AuditBlockchain();
            break;
        }
        case "22":
        {
            Console.WriteLine("=== Створені гаманці ===");
            foreach (var w in wallets.Values)
            {
                Console.WriteLine($"  {w.Name,-10} | Address: {w.Address} | PublicKey: {Convert.ToHexString(w.PublicKey)[..16]}...");
            }

            var signedTx = transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 10, aliceWallet);
            tempListTransactions.Add(signedTx);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[OK] Додано підписану транзакцію: From {signedTx.From} To {signedTx.To} Amount {signedTx.Amount}");
            Console.WriteLine($"     Signature: {Convert.ToHexString(signedTx.Signature)[..24]}...");
            Console.ResetColor();

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для повернення в меню...");
                Console.ReadLine();
            }
            break;
        }
        case "23":
        {
            var sigChain = new BlockChain(3);
            sigChain.SetMiningMode(blockChainService.GetMiningMode());

            if (!Console.IsOutputRedirected) Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #5 — РІВЕНЬ 1: ВАЛІДНА ПІДПИСАНА ТРАНЗАКЦІЯ ТА ПІДРОБКА СУМИ");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            Console.WriteLine("Створені гаманці:");
            Console.WriteLine($"  Alice   | Address: {aliceWallet.Address} | PublicKey: {Convert.ToHexString(aliceWallet.PublicKey)[..20]}...");
            Console.WriteLine($"  Bob     | Address: {bobWallet.Address} | PublicKey: {Convert.ToHexString(bobWallet.PublicKey)[..20]}...");
            Console.WriteLine($"  Charlie | Address: {charlieWallet.Address} | PublicKey: {Convert.ToHexString(charlieWallet.PublicKey)[..20]}...\n");

            // 1. Валідна транзакція Alice -> Bob (10)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1) Валідна транзакція: Alice -> Bob (Amount = 10), підпис alice.Sign(...)");
            Console.ResetColor();
            var validTx = transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 10m, aliceWallet);
            int countBefore = sigChain.Chain.Count;
            sigChain.AddBlock(new List<Transactions> { validTx });
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   [OK] Блоків до: {countBefore} -> після: {sigChain.Chain.Count} (транзакцію прийнято!)\n");
            Console.ResetColor();

            // 2. Підробка суми після підписання
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("2) Підробка №1 (зміна суми): Alice підписує 50, зловмисник змінює Amount на 999999");
            Console.ResetColor();
            var tamperedAmountTx = transactionService.CreateTransaction(aliceWallet.Address, bobWallet.Address, 50m, aliceWallet);
            Console.WriteLine($"   Підписана сума: {tamperedAmountTx.Amount}");
            tamperedAmountTx.Amount = 999999m;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   Підмінена сума: {tamperedAmountTx.Amount}");
            Console.ResetColor();
            countBefore = sigChain.Chain.Count;
            sigChain.AddBlock(new List<Transactions> { tamperedAmountTx });
            Console.WriteLine($"   Блоків до: {countBefore} -> після: {sigChain.Chain.Count} (підробку відхилено!)\n");

            if (!Console.IsInputRedirected)
            {
                Console.Write("Натисніть Enter для переходу до Рівня 2 (підпис чужим гаманцем)...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #5 — РІВЕНЬ 2: ПІДРОБКА ПІДПИСУ ЧУЖИМ ГАМАНЦЕМ (bob.Sign для Alice)");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            // 3. Рівень 2А: From = alice.Address, PublicKey = alice.PublicKey, але підпис bob.Sign(...)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("3) Атака 2А: From = alice.Address, PublicKey = alice.PublicKey, підпис = bob.Sign(...)");
            Console.ResetColor();
            var forgedTxA = new Transactions(aliceWallet.Address, bobWallet.Address, 100m, aliceWallet.PublicKey);
            forgedTxA.Signature = bobWallet.Sign(forgedTxA.GetDataToSign());
            Console.WriteLine($"   Транзакція: From={forgedTxA.From} -> To={forgedTxA.To} | Amount={forgedTxA.Amount}");
            Console.WriteLine($"   Підписано гаманцем: Bob (Signature: {Convert.ToHexString(forgedTxA.Signature)[..24]}...)");
            countBefore = sigChain.Chain.Count;
            sigChain.AddBlock(new List<Transactions> { forgedTxA });
            Console.WriteLine($"   Блоків до: {countBefore} -> після: {sigChain.Chain.Count} (блокчейн відхилив чужий підпис!)\n");

            // 4. Рівень 2Б: From = alice.Address, PublicKey = bob.PublicKey, підпис = bob.Sign(...)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("4) Атака 2Б (хитра): From = alice.Address, але Боб підставляє СВІЙ bob.PublicKey + bob.Sign(...)");
            Console.ResetColor();
            var forgedTxB = new Transactions(aliceWallet.Address, bobWallet.Address, 250m, bobWallet.PublicKey);
            forgedTxB.Signature = bobWallet.Sign(forgedTxB.GetDataToSign());
            Console.WriteLine($"   Транзакція: From={forgedTxB.From} (Alice) -> To={forgedTxB.To} (Bob) | Amount={forgedTxB.Amount}");
            Console.WriteLine($"   Підкладено PublicKey: Bob (адреса з ключа: {walletService.GetAddress(forgedTxB.PublicKey)})");
            Console.WriteLine($"   Підписано гаманцем:   Bob (Signature: {Convert.ToHexString(forgedTxB.Signature)[..24]}...)");
            countBefore = sigChain.Chain.Count;
            sigChain.AddBlock(new List<Transactions> { forgedTxB });
            Console.WriteLine($"   Блоків до: {countBefore} -> після: {sigChain.Chain.Count} (блокчейн помітив невідповідність From і PublicKey!)\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("5) Фінальний аудит блокчейну (AuditBlockchain()):");
            Console.ResetColor();
            sigChain.AuditBlockchain();
            break;
        }
        case "0":
        {
            if (backgroundMiningTask != null && !backgroundMiningTask.IsCompleted)
            {
                Console.WriteLine("Очікуємо завершення фонового майнінгу перед виходом...");
                await backgroundMiningTask;
            }
            running = false;
            break;
        }
        default:
        {
            Console.WriteLine("Невірний вибір, спробуйте ще раз.");
            break;
        }
    }
}
