var lines = new[] { new OrderLine("P001", 5) };

// 內容相同，但兩個 Order 使用不同的陣列物件。
var a = new Order("ORD-001", lines);
var b = new Order("ORD-001", lines.ToArray());

Console.WriteLine($"record 預設相等性: {a == b}"); // False
Console.WriteLine(
    $"逐一比較集合內容: {a.Lines.SequenceEqual(b.Lines)}"); // True

public sealed record Order(
    string Id,
    IReadOnlyList<OrderLine> Lines
);

public sealed record OrderLine(
    string ProductId,
    int Quantity
);
