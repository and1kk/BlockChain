namespace BlockChainP33;

public class Block
{
    public int Index { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string PrevHash { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int Nonce { get; set; } = 0;
}
