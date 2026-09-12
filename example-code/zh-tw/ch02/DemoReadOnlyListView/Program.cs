Console.WriteLine("=== 示範 IReadOnlyList<T> 只是唯讀檢視 ===");

var lines = new List<OrderLine>();
var order = new Order("ORD-001", lines);

Console.WriteLine($"外部修改前: order.Lines.Count = {order.Lines.Count}");

// order.Lines.Add(new OrderLine("P001", 5)); // 編譯錯誤!

// 外部仍然握有原本的 List<T> 參考，所以仍可修改內容。
lines.Add(new OrderLine("P001", 5));

Console.WriteLine($"外部修改後: order.Lines.Count = {order.Lines.Count}");

public record OrderLine(string ProductId, int Quantity);

public record Order(
    string Id,
    IReadOnlyList<OrderLine> Lines
);
