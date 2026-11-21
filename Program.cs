using BlockChainP33;
using Models;
using Service;

var blockChainService = new BlockChain(4);
var displayService = new DisplayService();
var transactionService = new TransactionService();

var tempListTransactions = new List<Transactions>
{
    new Transactions("Alex", "Bob", 100)
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
    Console.WriteLine("17. [ДЗ #3] Власний Blockchain історії подій (5 блоків + Explorer + IsValid)");
    Console.WriteLine("18. [ДЗ #3] Додати подію (блок із Data)");
    Console.WriteLine("19. [ДЗ #3] Blockchain Explorer (пошук блоку за Index або текстом у Data)");
    Console.WriteLine("20. [ДЗ #3] Перевірка безпеки IsValid() та тест зміни Data у 2-му блоці");
    Console.WriteLine("0. Вихід");
    Console.Write("Оберіть дію: ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    switch (choice)
    {
        case "1":
        {
            var sample = sampleTransfers[txCounter % sampleTransfers.Length];
            txCounter++;
            var tx = transactionService.CreateTransaction(sample.From, sample.To, sample.Amount);
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
                var tx = transactionService.CreateTransaction(sample.From, sample.To, sample.Amount);
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
            Console.Write("Від кого: ");
            string from = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Кому: ");
            string to = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Скільки: ");
            string? amountInput = Console.ReadLine();

            if (!decimal.TryParse(amountInput, out decimal amount))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Невірний формат суми.");
                Console.ResetColor();
                break;
            }

            var newTx = transactionService.CreateTransaction(from, to, amount);
            var (isValid, message) = transactionService.ValidateTransaction(newTx);

            if (!isValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Помилка] {message}");
                Console.ResetColor();
                break;
            }

            tempListTransactions.Add(newTx);
            PrintTransactionInfo(newTx, "Нова транзакція");
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
                new Transactions("Alex", "Bob", 10),
                new Transactions("Bob", "Charlie", 20),
                new Transactions("Charlie", "Dave", 30),
                new Transactions("Dave", "Eve", -50), // Невалідна сума!
                new Transactions("Eve", "Frank", 40)
            };
            blockChainService.AddBlock(listWithInvalid);
            Console.WriteLine($"Кількість блоків після Тесту 1: {blockChainService.Chain.Count}\n");

            // Тест 2: Дублікат ID всередині одного нового блоку
            Console.WriteLine("--- Тест 2: Повторення ID всередині нового блоку ---");
            var tx1 = new Transactions("Alex", "Bob", 10);
            var tx2 = new Transactions("Bob", "Charlie", 20) { Id = tx1.Id }; // Такий самий ID!
            blockChainService.AddBlock(new List<Transactions> { tx1, tx2 });
            Console.WriteLine($"Кількість блоків після Тесту 2: {blockChainService.Chain.Count}\n");

            // Тест 3: Транзакція з таким ID вже існує в Blockchain
            Console.WriteLine("--- Тест 3: Транзакція вже існує в Blockchain ---");
            var existingTx = new Transactions("Alex", "Bob", 100);
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
            // Рівень 1: Створення ланцюга з 5 блоками різних подій
            var eventChain = new BlockChain(3);
            eventChain.SetMiningMode(blockChainService.GetMiningMode());

            string[] events =
            {
                "Реєстрація користувача: Олександр (alex@example.com)",
                "Поповнення балансу: Олександр +5000 UAH",
                "Покупка товару: Ноутбук Lenovo ThinkPad (3200 UAH)",
                "Переказ коштів: Олександр -> Марія (1200 UAH)",
                "Налаштування безпеки: Активація 2FA для користувача Олександр"
            };

            foreach (var ev in events)
            {
                eventChain.AddEventBlock(ev);
            }

            if (!Console.IsOutputRedirected) Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 1: СТВОРЕННЯ ЛАНЦЮГА (5 БЛОКІВ ІЗ РІЗНИМИ ПОДІЯМИ)");
            Console.WriteLine("===========================================================================");
            Console.ResetColor();
            displayService.DisplayChain(eventChain);

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для переходу до Рівня 2 (Blockchain Explorer)...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            // Рівень 2: Blockchain Explorer
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 2: BLOCKCHAIN EXPLORER (ПОШУК ЗА INDEX ТА DATA)");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();
            displayService.SearchBlockByIndex(eventChain, 3);
            Console.WriteLine();
            displayService.SearchBlocksByData(eventChain, "Переказ коштів");
            Console.WriteLine();
            displayService.SearchBlocksByData(eventChain, "Покупка");

            if (!Console.IsInputRedirected)
            {
                Console.Write("\nНатисніть Enter для переходу до Рівня 3 (Перевірка безпеки IsValid)...");
                Console.ReadLine();
                if (!Console.IsOutputRedirected) Console.Clear();
            }

            // Рівень 3: Перевірка безпеки IsValid() до і після зміни Data у 2-му блоці
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("===========================================================================");
            Console.WriteLine("  ДЗ #3 — РІВЕНЬ 3: ПЕРЕВІРКА БЕЗПЕКИ IsValid() ТА ПІДРОБКА БЛОКУ #2");
            Console.WriteLine("===========================================================================\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("1) Перевірка цілісності оригінального ланцюга (IsValid()):");
            Console.ResetColor();
            eventChain.IsValid();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n2) Втручання у дані: змінюємо Data у другому блоці (Index = 2)...");
            Console.WriteLine($"   Було:  {eventChain.Chain[2].Data}");
            eventChain.Chain[2].Data = "Поповнення балансу: Олександр +999999 UAH [ПІДРОБКА]";
            Console.WriteLine($"   Стало: {eventChain.Chain[2].Data}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n3) Повторна перевірка цілісності після зміни Data (IsValid()):");
            Console.ResetColor();
            eventChain.IsValid();

            Console.WriteLine("\nСтан пошкодженого блоку #2:");
            displayService.DisplayBlockDetails(eventChain.Chain[2]);
            break;
        }
        case "18":
        {
            Console.Write("Введіть опис події (Data) для нового блоку: ");
            string? eventData = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(eventData))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Помилка] Опис події не може бути порожнім.");
                Console.ResetColor();
                break;
            }

            var addedBlock = blockChainService.AddEventBlock(eventData);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Подію додано у блок #{addedBlock.Index}!");
            Console.ResetColor();
            displayService.DisplayBlockDetails(addedBlock);
            break;
        }
        case "19":
        {
            Console.WriteLine("=== Blockchain Explorer ===");
            Console.WriteLine("1. Пошук блоку за Index");
            Console.WriteLine("2. Пошук подій за текстом у Data");
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
                Console.Write("Введіть текст для пошуку в Data: ");
                string? query = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(query))
                {
                    displayService.SearchBlocksByData(blockChainService, query);
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
        case "20":
        {
            Console.WriteLine("Перевірка поточного ланцюга методом IsValid():");
            blockChainService.IsValid();

            if (blockChainService.Chain.Count > 2)
            {
                Console.Write("Змінити Data у блоці #2 для перевірки безпеки? (y/n): ");
                string? ans = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (ans == "y" || ans == "yes" || ans == "так" || ans == "д")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Злом] Старе Data блоку #2: {blockChainService.Chain[2].Data}");
                    blockChainService.Chain[2].Data = "ПІДРОБЛЕНІ ДАНІ У БЛОЦІ #2";
                    Console.WriteLine($"[Злом] Нове Data блоку #2:  {blockChainService.Chain[2].Data}");
                    Console.ResetColor();
                    blockChainService.IsValid();
                }
            }
            else
            {
                Console.WriteLine("Для тесту зміни 2-го блоку додайте хоча б 2 блоки.");
            }
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
