namespace BlockChainP33;

public class BlockChain
{
    public List<Block> Chain { get; set; }
    private readonly HashingService _hashingService;
    private readonly MiningService _miningService;
    public string TargetPrefix { get; set; } = "and";

    public BlockChain(string targetPrefix = "and")
    {
        Chain = new List<Block>();
        TargetPrefix = targetPrefix;
        _hashingService = new HashingService();
        _miningService = new MiningService();
        AddGenesisBlock();
    }

    private void AddGenesisBlock()
    {
        var genesisBlock = new Block
        {
            Index = 0,
            Author = "andrey",
            Data = "Genesis Block",
            PrevHash = "andrey",
            TimeStamp = DateTime.Parse("2023-06-01"),
            Nonce = 0
        };
        genesisBlock.Hash = _hashingService.ComputeBlockHash(genesisBlock);
        _miningService.MineBlock(genesisBlock, TargetPrefix);
        Chain.Add(genesisBlock);
    }

    public void AddBlock(string data, string author = "andrey")
    {
        var lastBlock = Chain[Chain.Count - 1];
        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Data = data,
            Author = author,
            PrevHash = lastBlock.Hash,
            TimeStamp = DateTime.Now,
            Nonce = 0
        };
        newBlock.Hash = _hashingService.ComputeBlockHash(newBlock);
        _miningService.MineBlock(newBlock, TargetPrefix);
        Chain.Add(newBlock);
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
            if (!currentBlock.Hash.StartsWith(TargetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
