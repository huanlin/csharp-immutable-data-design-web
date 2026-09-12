using System;
using System.Collections.Generic;

public record OrderLine(string ProductId, int Quantity);

public record Order(
    string Id,
    List<OrderLine> Lines // List<T> 是可變的
);

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 示範 淺層不可變陷阱 ===");

        var order = new Order("ORD-001", new List<OrderLine>
        {
            new("P001", 2)
        });

        Console.WriteLine($"修改前項目數量: {order.Lines.Count}");

        // 不能替換整個 Lines 參考。
        // order.Lines = new List<OrderLine>(); // 編譯錯誤!

        // 雖然 order 是 record 且屬性不能被替換，但內部的 List 可以被修改
        order.Lines.Add(new OrderLine("P002", 5));

        Console.WriteLine($"修改後項目數量: {order.Lines.Count}"); // 變成 2！
    }
}
