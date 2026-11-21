namespace BlockChainP33;

public class Block
{
    public int Index { get; set; }
    public string Data { get; set; } = string.Empty;
    public List<Models.Transactions> Transactions { get; set; } = new List<Models.Transactions>();
    public string PrevHash { get; set; } = string.Empty;
    public string PreviousHash
    {
        get => PrevHash;
        set => PrevHash = value;
    }
    public DateTime TimeStamp { get; set; }
    public DateTime Timestamp
    {
        get => TimeStamp;
        set => TimeStamp = value;
    }
    public string Hash { get; set; } = string.Empty;
    public int Nonce { get; set; } = 0;
    public long TimeStampUnix => new DateTimeOffset(TimeStamp).ToUnixTimeSeconds();
    public decimal MiningDuration { get; set; }
    public int DifficultyAtMining { get; set; }
    public int Attempts { get; set; }

}
