using System;

// 定義 Positional Record
public record OrderLine(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 示範 Positional Record ===");

        // 1. 初始化
        var line = new OrderLine("P001", "C# 不可變資料設計", 2, 580m);
        Console.WriteLine($"原始物件: {line}");

        // 2. 解構 (Deconstruction)
        var (productId, productName, quantity, unitPrice) = line;
        Console.WriteLine($"解構結果: {productName} ({productId}) x {quantity} = {unitPrice * quantity:C}");
    }
}
