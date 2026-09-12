using System;

// 傳統類別（參考比較）
public class OrderLineClass
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }

    public OrderLineClass(string productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}

// 記錄型別（值比較）
public record OrderLineRecord(string ProductId, int Quantity);

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 示範 Value Equality ===");

        // 1. class 比較：參考相同才相同
        var c1 = new OrderLineClass("P001", 5);
        var c2 = new OrderLineClass("P001", 5);
        Console.WriteLine($"class (c1 == c2): {c1 == c2}"); // False
        Console.WriteLine($"class Equals: {c1.Equals(c2)}"); // False

        // 2. record 比較：值相同即相同
        var r1 = new OrderLineRecord("P001", 5);
        var r2 = new OrderLineRecord("P001", 5);
        Console.WriteLine($"record (r1 == r2): {r1 == r2}"); // True
        Console.WriteLine($"record Equals: {r1.Equals(r2)}"); // True
    }
}
