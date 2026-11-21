using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using ILGPU.Runtime.CPU;

namespace BlockChainP33;

public enum MiningMode
{
    CpuSingleThread = 1,
    CpuMultiThread = 2,
    GpuSingleThread = 3,
    GpuMultiThread = 4
}

public class MiningService
{
    private Context? _gpuContext;
    private Accelerator? _gpuAccelerator;
    private Action<Index1D, ArrayView<byte>, int, int, ArrayView<int>>? _gpuKernel;
    private Action<AcceleratorStream, Index1D, ArrayView<byte>, int, int, ArrayView<int>>? _gpuStreamKernel;
    private readonly object _gpuInitLock = new();

    public MiningMode CurrentMode { get; set; } = MiningMode.GpuMultiThread;

    private void EnsureGpuInitialized()
    {
        if (_gpuContext != null && _gpuAccelerator != null && _gpuKernel != null && _gpuStreamKernel != null)
            return;

        lock (_gpuInitLock)
        {
            if (_gpuContext == null || _gpuAccelerator == null || _gpuKernel == null || _gpuStreamKernel == null)
            {
                _gpuContext = Context.Create(builder => builder.Cuda().OpenCL().CPU());
                var device = _gpuContext.GetPreferredDevice(preferCPU: false);
                _gpuAccelerator = device.CreateAccelerator(_gpuContext);
                _gpuKernel = _gpuAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<byte>, int, int, ArrayView<int>>(Sha256GpuKernel);
                _gpuStreamKernel = _gpuAccelerator.LoadAutoGroupedKernel<Index1D, ArrayView<byte>, int, int, ArrayView<int>>(Sha256GpuKernel);
            }
        }
    }

    public void MineBlock(Block block, int difficulty = 3)
    {
        switch (CurrentMode)
        {
            case MiningMode.CpuSingleThread:
                MineBlockCpuSingleThread(block, difficulty);
                break;
            case MiningMode.CpuMultiThread:
                MineBlockCpuMultiThread(block, difficulty);
                break;
            case MiningMode.GpuSingleThread:
                MineBlockGpuSingleThread(block, difficulty);
                break;
            case MiningMode.GpuMultiThread:
            default:
                MineBlockGpuMultiThread(block, difficulty);
                break;
        }
    }

    // Асинхронний майнінг, щоб програма залишалася доступною під час пошуку хешу
    public Task MineBlockAsync(Block block, int difficulty = 3, MiningMode? mode = null, CancellationToken cancellationToken = default)
    {
        var selectedMode = mode ?? CurrentMode;
        return Task.Run(() =>
        {
            switch (selectedMode)
            {
                case MiningMode.CpuSingleThread:
                    MineBlockCpuSingleThread(block, difficulty, cancellationToken);
                    break;
                case MiningMode.CpuMultiThread:
                    MineBlockCpuMultiThread(block, difficulty, 0, cancellationToken);
                    break;
                case MiningMode.GpuSingleThread:
                    MineBlockGpuSingleThread(block, difficulty, cancellationToken);
                    break;
                case MiningMode.GpuMultiThread:
                default:
                    MineBlockGpuMultiThread(block, difficulty, 4, cancellationToken);
                    break;
            }
        }, cancellationToken);
    }

    // === 1. ПРОСТО ЧЕРЕЗ ПРОЦЕСОР (CPU 1 потік — звичайний послідовний майнінг) ===
    public void MineBlockCpuSingleThread(Block block, int difficulty = 3, CancellationToken cancellationToken = default)
    {
        string target = new string('0', difficulty);
        block.DifficultyAtMining = difficulty;
        var stopwatch = Stopwatch.StartNew();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[CPU 1-Thread] Старт звичайного майнінгу блока #{block.Index} в 1 потік (Difficulty: {difficulty}, Шукаємо: '{target}')...");
        Console.ResetColor();

        var stringTransactions = string.Concat(block.Transactions.Select(t => t.ToRawString()));
        string prefixStr = $"{block.Index}{block.TimeStamp}{block.PrevHash}{block.Data}{stringTransactions}{block.DifficultyAtMining}";

        using var sha256 = SHA256.Create();
        int winningNonce = 0;
        string winningHash = string.Empty;

        for (int nonce = 0; nonce < int.MaxValue; nonce++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            byte[] bytes = Encoding.UTF8.GetBytes(prefixStr + nonce);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            string hash = Convert.ToHexString(hashBytes);

            if (hash.StartsWith(target, StringComparison.Ordinal))
            {
                winningNonce = nonce;
                winningHash = hash;
                break;
            }
        }

        stopwatch.Stop();
        block.Nonce = winningNonce;
        block.Attempts = winningNonce + 1;
        block.Hash = winningHash;
        block.MiningDuration = Math.Round((decimal)stopwatch.Elapsed.TotalSeconds, 4);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[CPU 1-Thread] Блок #{block.Index} змайнено за {block.MiningDuration:F4} с! Спроб: {block.Attempts} | Nonce: {block.Nonce} | Hash: {block.Hash}\n");
        Console.ResetColor();
    }

    // === 2. ЧЕРЕЗ ПРОЦЕСОР З БАГАТОПОТОЧНІСТЮ (CPU Multi-Thread — паралельні потоки, кожен перевіряє свої Nonce) ===
    public void MineBlockCpuMultiThread(Block block, int difficulty = 3, int threadCount = 0, CancellationToken cancellationToken = default)
    {
        if (threadCount <= 0)
            threadCount = Environment.ProcessorCount;

        string target = new string('0', difficulty);
        block.DifficultyAtMining = difficulty;
        var stopwatch = Stopwatch.StartNew();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[CPU Multi-Thread] Старт багатопотокового майнінгу блока #{block.Index} у {threadCount} потоків (Difficulty: {difficulty}, Шукаємо: '{target}')...");
        Console.ResetColor();

        var stringTransactions = string.Concat(block.Transactions.Select(t => t.ToRawString()));
        string prefixStr = $"{block.Index}{block.TimeStamp}{block.PrevHash}{block.Data}{stringTransactions}{block.DifficultyAtMining}";

        int winningNonce = int.MaxValue;
        string winningHash = string.Empty;
        long totalAttempts = 0;
        object lockObj = new();

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                using var sha256 = SHA256.Create();
                long localAttempts = 0;

                // Кожен потік перевіряє свої значення Nonce: threadId, threadId + threadCount, ...
                for (long nonce = threadId; nonce < int.MaxValue; nonce += threadCount)
                {
                    if (Volatile.Read(ref winningNonce) != int.MaxValue || cancellationToken.IsCancellationRequested)
                        break;

                    int currentNonce = (int)nonce;
                    byte[] bytes = Encoding.UTF8.GetBytes(prefixStr + currentNonce);
                    byte[] hashBytes = sha256.ComputeHash(bytes);
                    localAttempts++;

                    string hash = Convert.ToHexString(hashBytes);
                    if (hash.StartsWith(target, StringComparison.Ordinal))
                    {
                        lock (lockObj)
                        {
                            if (currentNonce < winningNonce)
                            {
                                winningNonce = currentNonce;
                                winningHash = hash;
                            }
                        }
                        break; // Хеш знайдено — зупиняємо цей потік та сигналізуємо іншим через winningNonce
                    }
                }

                Interlocked.Add(ref totalAttempts, localAttempts);
            }, cancellationToken);
        }

        Task.WaitAll(tasks);
        stopwatch.Stop();

        if (winningNonce == int.MaxValue)
            winningNonce = 0;

        block.Nonce = winningNonce;
        block.Attempts = (int)Math.Min(totalAttempts, int.MaxValue);
        var hashingService = new HashingService();
        block.Hash = !string.IsNullOrEmpty(winningHash) ? winningHash : hashingService.ComputeBlockHash(block);
        block.MiningDuration = Math.Round((decimal)stopwatch.Elapsed.TotalSeconds, 4);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[CPU Multi-Thread] Блок #{block.Index} змайнено ({threadCount} потоків) за {block.MiningDuration:F4} с! Спроб: {block.Attempts} | Nonce: {block.Nonce} | Hash: {block.Hash}\n");
        Console.ResetColor();
    }

    // === 3. ЧЕРЕЗ ВІДЕОКАРТУ (GPU 1-Stream — послідовна подача батчів з 1 потоку) ===
    public void MineBlockGpuSingleThread(Block block, int difficulty = 3, CancellationToken cancellationToken = default)
    {
        EnsureGpuInitialized();
        string target = new string('0', difficulty);
        block.DifficultyAtMining = difficulty;
        var stopwatch = Stopwatch.StartNew();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[GPU 1-Stream] Пристрій: {_gpuAccelerator!.Name} ({_gpuAccelerator.AcceleratorType})");
        Console.WriteLine($"[GPU 1-Stream] Старт майнінгу блока #{block.Index} в 1 потік подачі (Difficulty: {difficulty}, Шукаємо: '{target}')...");
        Console.ResetColor();

        var stringTransactions = string.Concat(block.Transactions.Select(t => t.ToRawString()));
        string prefixStr = $"{block.Index}{block.TimeStamp}{block.PrevHash}{block.Data}{stringTransactions}{block.DifficultyAtMining}";
        byte[] prefixBytes = Encoding.UTF8.GetBytes(prefixStr);

        using var gpuPrefix = _gpuAccelerator.Allocate1D(prefixBytes);
        using var gpuResult = _gpuAccelerator.Allocate1D<int>(1);
        gpuResult.CopyFromCPU(new int[] { int.MaxValue });

        int batchSize = 65_536; // 1 потік подає батчі по 65 536 Nonce послідовно
        int winningNonce = 0;
        int[] resultBuffer = new int[1];

        for (long startNonce = 0; startNonce < int.MaxValue; startNonce += batchSize)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            int currentBatch = (int)Math.Min(batchSize, (long)int.MaxValue - startNonce);
            _gpuKernel!(currentBatch, gpuPrefix.View, (int)startNonce, difficulty, gpuResult.View);
            _gpuAccelerator.Synchronize();

            gpuResult.CopyToCPU(resultBuffer);
            if (resultBuffer[0] != int.MaxValue)
            {
                winningNonce = resultBuffer[0];
                break;
            }
        }

        block.Nonce = winningNonce;
        block.Attempts = winningNonce + 1;
        var hashingService = new HashingService();
        block.Hash = hashingService.ComputeBlockHash(block);

        stopwatch.Stop();
        block.MiningDuration = Math.Round((decimal)stopwatch.Elapsed.TotalSeconds, 4);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[GPU 1-Stream] Блок #{block.Index} змайнено на GPU за {block.MiningDuration:F4} с! Nonce: {block.Nonce} | Hash: {block.Hash}\n");
        Console.ResetColor();
    }

    // === 4. ЧЕРЕЗ ВІДЕОКАРТУ З ПОТОКАМИ (GPU Multi-Thread / Multi-Stream — паралельні потоки подають свої діапазони Nonce у паралельні CUDA-стріми) ===
    public void MineBlockGpuMultiThread(Block block, int difficulty = 3, int workerStreams = 4, CancellationToken cancellationToken = default)
    {
        EnsureGpuInitialized();
        string target = new string('0', difficulty);
        block.DifficultyAtMining = difficulty;
        var stopwatch = Stopwatch.StartNew();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[GPU Multi-Stream] Пристрій: {_gpuAccelerator!.Name} ({_gpuAccelerator.AcceleratorType}) | Потоків/Стрімів: {workerStreams}");
        Console.WriteLine($"[GPU Multi-Stream] Старт багатопотокового GPU-майнінгу блока #{block.Index} (Difficulty: {difficulty}, Шукаємо: '{target}')...");
        Console.ResetColor();

        var stringTransactions = string.Concat(block.Transactions.Select(t => t.ToRawString()));
        string prefixStr = $"{block.Index}{block.TimeStamp}{block.PrevHash}{block.Data}{stringTransactions}{block.DifficultyAtMining}";
        byte[] prefixBytes = Encoding.UTF8.GetBytes(prefixStr);

        using var gpuPrefix = _gpuAccelerator.Allocate1D(prefixBytes);
        using var gpuResult = _gpuAccelerator.Allocate1D<int>(1);
        gpuResult.CopyFromCPU(new int[] { int.MaxValue });

        int batchSizePerStream = 524_288; // 524 288 потоків GPU за батч у кожному паралельному стрімі
        int globalWinningNonce = int.MaxValue;
        long nextStartNonce = 0;

        var tasks = new Task[workerStreams];
        for (int w = 0; w < workerStreams; w++)
        {
            tasks[w] = Task.Run(() =>
            {
                using var stream = _gpuAccelerator.CreateStream();
                int[] localResult = new int[1];

                while (Volatile.Read(ref globalWinningNonce) == int.MaxValue && !cancellationToken.IsCancellationRequested)
                {
                    long myStartNonce = Interlocked.Add(ref nextStartNonce, batchSizePerStream) - batchSizePerStream;
                    if (myStartNonce >= int.MaxValue)
                        break;

                    int currentBatch = (int)Math.Min(batchSizePerStream, (long)int.MaxValue - myStartNonce);
                    _gpuStreamKernel!(stream, currentBatch, gpuPrefix.View, (int)myStartNonce, difficulty, gpuResult.View);
                    stream.Synchronize();

                    gpuResult.CopyToCPU(stream, localResult);
                    stream.Synchronize();

                    if (localResult[0] != int.MaxValue)
                    {
                        int currentBest;
                        do
                        {
                            currentBest = Volatile.Read(ref globalWinningNonce);
                            if (localResult[0] >= currentBest) break;
                        } while (Interlocked.CompareExchange(ref globalWinningNonce, localResult[0], currentBest) != currentBest);
                        break;
                    }
                }
            }, cancellationToken);
        }

        Task.WaitAll(tasks);
        _gpuAccelerator.Synchronize();

        int[] finalResult = new int[1];
        gpuResult.CopyToCPU(finalResult);
        int winningNonce = finalResult[0] == int.MaxValue ? 0 : finalResult[0];
        block.Nonce = winningNonce;
        block.Attempts = winningNonce + 1;
        var hashingService = new HashingService();
        block.Hash = hashingService.ComputeBlockHash(block);

        stopwatch.Stop();
        block.MiningDuration = Math.Round((decimal)stopwatch.Elapsed.TotalSeconds, 4);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[GPU Multi-Stream] Блок #{block.Index} змайнено на GPU ({workerStreams} паралельних потоків/стрімів) за {block.MiningDuration:F4} с! Nonce: {block.Nonce} | Hash: {block.Hash}\n");
        Console.ResetColor();
    }

    // Ядро (Kernel), яке виконується паралельно на тисячах ядер відеокарти
    private static void Sha256GpuKernel(
        Index1D index,
        ArrayView<byte> prefix,
        int startNonce,
        int difficulty,
        ArrayView<int> result)
    {
        int nonce = startNonce + index.X;
        if (nonce >= result[0])
            return;
        int prefixLen = (int)prefix.Length;

        int nonceLen = GetNonceLength(nonce);
        int totalLen = prefixLen + nonceLen;
        int numChunks = (totalLen + 9 + 63) / 64;
        int paddedLen = numChunks * 64;

        uint h0 = 0x6a09e667u;
        uint h1 = 0xbb67ae85u;
        uint h2 = 0x3c6ef372u;
        uint h3 = 0xa54ff53au;
        uint h4 = 0x510e527fu;
        uint h5 = 0x9b05688cu;
        uint h6 = 0x1f83d9abu;
        uint h7 = 0x5be0cd19u;

        uint[] w = new uint[64];

        for (int chunk = 0; chunk < numChunks; chunk++)
        {
            int chunkOffset = chunk * 64;
            for (int i = 0; i < 16; i++)
            {
                int b0Pos = chunkOffset + i * 4;
                uint b0 = GetPaddedByte(b0Pos, prefix, prefixLen, nonce, nonceLen, totalLen, paddedLen);
                uint b1 = GetPaddedByte(b0Pos + 1, prefix, prefixLen, nonce, nonceLen, totalLen, paddedLen);
                uint b2 = GetPaddedByte(b0Pos + 2, prefix, prefixLen, nonce, nonceLen, totalLen, paddedLen);
                uint b3 = GetPaddedByte(b0Pos + 3, prefix, prefixLen, nonce, nonceLen, totalLen, paddedLen);
                w[i] = (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }

            for (int i = 16; i < 64; i++)
            {
                uint s0 = RotateRight(w[i - 15], 7) ^ RotateRight(w[i - 15], 18) ^ (w[i - 15] >> 3);
                uint s1 = RotateRight(w[i - 2], 17) ^ RotateRight(w[i - 2], 19) ^ (w[i - 2] >> 10);
                w[i] = w[i - 16] + s0 + w[i - 7] + s1;
            }

            uint a = h0, b = h1, c = h2, d = h3, e = h4, f = h5, g = h6, h = h7;

            for (int i = 0; i < 64; i++)
            {
                uint S1 = RotateRight(e, 6) ^ RotateRight(e, 11) ^ RotateRight(e, 25);
                uint ch = (e & f) ^ ((~e) & g);
                uint temp1 = h + S1 + ch + GetK(i) + w[i];
                uint S0 = RotateRight(a, 2) ^ RotateRight(a, 13) ^ RotateRight(a, 22);
                uint maj = (a & b) ^ (a & c) ^ (b & c);
                uint temp2 = S0 + maj;

                h = g;
                g = f;
                f = e;
                e = d + temp1;
                d = c;
                c = b;
                b = a;
                a = temp1 + temp2;
            }

            h0 += a; h1 += b; h2 += c; h3 += d;
            h4 += e; h5 += f; h6 += g; h7 += h;
        }

        // Перевірка перших 'difficulty' шестнадцяткових символів (по 4 біти на кожен '0')
        bool match = true;
        for (int k = 0; k < difficulty; k++)
        {
            uint word = (k < 8) ? h0 : (k < 16) ? h1 : (k < 24) ? h2 : h3;
            int shift = (7 - (k & 7)) * 4;
            if (((word >> shift) & 0xFu) != 0u)
            {
                match = false;
                break;
            }
        }

        if (match)
        {
            Atomic.Min(ref result[0], nonce);
        }
    }

    private static uint RotateRight(uint value, int bits) => (value >> bits) | (value << (32 - bits));

    private static int GetNonceLength(int nonce)
    {
        if (nonce < 10) return 1;
        if (nonce < 100) return 2;
        if (nonce < 1_000) return 3;
        if (nonce < 10_000) return 4;
        if (nonce < 100_000) return 5;
        if (nonce < 1_000_000) return 6;
        if (nonce < 10_000_000) return 7;
        if (nonce < 100_000_000) return 8;
        if (nonce < 1_000_000_000) return 9;
        return 10;
    }

    private static int Pow10(int exp) => exp switch
    {
        0 => 1,
        1 => 10,
        2 => 100,
        3 => 1_000,
        4 => 10_000,
        5 => 100_000,
        6 => 1_000_000,
        7 => 10_000_000,
        8 => 100_000_000,
        _ => 1_000_000_000
    };

    private static byte GetPaddedByte(
        int pos,
        ArrayView<byte> prefix,
        int prefixLen,
        int nonce,
        int nonceLen,
        int totalLen,
        int paddedLen)
    {
        if (pos < prefixLen)
            return prefix[pos];
        if (pos < totalLen)
        {
            int digitIndex = pos - prefixLen;
            int divisor = Pow10(nonceLen - 1 - digitIndex);
            return (byte)('0' + ((nonce / divisor) % 10));
        }
        if (pos == totalLen)
            return 0x80;
        if (pos >= paddedLen - 8)
        {
            ulong bitLen = (ulong)totalLen * 8UL;
            int shift = (paddedLen - 1 - pos) * 8;
            return (byte)((bitLen >> shift) & 0xFFUL);
        }
        return 0;
    }

    private static uint GetK(int i) => i switch
    {
        0 => 0x428a2f98u, 1 => 0x71374491u, 2 => 0xb5c0fbcfu, 3 => 0xe9b5dba5u,
        4 => 0x3956c25bu, 5 => 0x59f111f1u, 6 => 0x923f82a4u, 7 => 0xab1c5ed5u,
        8 => 0xd807aa98u, 9 => 0x12835b01u, 10 => 0x243185beu, 11 => 0x550c7dc3u,
        12 => 0x72be5d74u, 13 => 0x80deb1feu, 14 => 0x9bdc06a7u, 15 => 0xc19bf174u,
        16 => 0xe49b69c1u, 17 => 0xefbe4786u, 18 => 0x0fc19dc6u, 19 => 0x240ca1ccu,
        20 => 0x2de92c6fu, 21 => 0x4a7484aau, 22 => 0x5cb0a9dcu, 23 => 0x76f988dau,
        24 => 0x983e5152u, 25 => 0xa831c66du, 26 => 0xb00327c8u, 27 => 0xbf597fc7u,
        28 => 0xc6e00bf3u, 29 => 0xd5a79147u, 30 => 0x06ca6351u, 31 => 0x14292967u,
        32 => 0x27b70a85u, 33 => 0x2e1b2138u, 34 => 0x4d2c6dfcU, 35 => 0x53380d13u,
        36 => 0x650a7354u, 37 => 0x766a0abbu, 38 => 0x81c2c92eu, 39 => 0x92722c85u,
        40 => 0xa2bfe8a1u, 41 => 0xa81a664bu, 42 => 0xc24b8b70u, 43 => 0xc76c51a3u,
        44 => 0xd192e819u, 45 => 0xd6990624u, 46 => 0xf40e3585u, 47 => 0x106aa070u,
        48 => 0x19a4c116u, 49 => 0x1e376c08u, 50 => 0x2748774cu, 51 => 0x34b0bcb5u,
        52 => 0x391c0cb3u, 53 => 0x4ed8aa4au, 54 => 0x5b9cca4fu, 55 => 0x682e6ff3u,
        56 => 0x748f82eeu, 57 => 0x78a5636fu, 58 => 0x84c87814u, 59 => 0x8cc70208u,
        60 => 0x90befffaU, 61 => 0xa4506cebu, 62 => 0xbef9a3f7u, 63 => 0xc67178f2u,
        _ => 0u
    };
}
