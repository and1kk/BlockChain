using BlockChainP33;

var blockChainService = new BlockChain();
var displayService = new DisplayService();

blockChainService.AddBlock("alex send bob 100", "Alex");
blockChainService.AddBlock("bob send charlie 50", "Bob");
blockChainService.AddBlock("charlie send dave 25", "Charlie");
blockChainService.AddBlock("dave send eve 10", "Dave");
blockChainService.AddBlock("eve send frank 5", "Eve");

displayService.DisplayChain(blockChainService);
var isValid = blockChainService.IsChainValid();

if (isValid)
{
    Console.WriteLine("Blockchain is valid.");
}
else
{
    Console.WriteLine("Blockchain is not valid.");
}

Console.ReadLine();

blockChainService.Chain[2].Data = "bob send charlie 1000"; // Зміна даних блоку для демонстрації порушення цілісності

displayService.DisplayChain(blockChainService);

isValid = blockChainService.IsChainValid();
if (isValid)
{
    Console.WriteLine("Blockchain is valid.");
}
else
{
    Console.WriteLine("Blockchain is not valid.");
}
