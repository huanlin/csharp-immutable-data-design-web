using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public record OrderLine(string ProductId, int Quantity);

public sealed class SafeOrder
{
    public string Id { get; }
    public IReadOnlyList<OrderLine> Lines { get; }

    public SafeOrder(string id, IEnumerable<OrderLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Id = id;
        // 先複製，再以唯讀包裝避免呼叫方轉回陣列後修改。
        Lines = Array.AsReadOnly(lines.ToArray());
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 示範 唯讀與不可變集合 ===");

        // 1. 防禦性複製
        var sourceList = new List<OrderLine> { new("P001", 1) };
        var order = new SafeOrder("ORD-001", sourceList);
        sourceList.Add(new("P002", 5)); // 外部修改
        Console.WriteLine(
            $"SafeOrder 中的項目數量: {order.Lines.Count}"); // 1

        // 2. 不可變更新會回傳新的集合版本。
        var productIds = ImmutableList.Create("P001", "P002");
        var updatedProductIds = productIds.Add("P003");
        Console.WriteLine($"原始 ImmutableList 項目數量: {productIds.Count}");
        Console.WriteLine($"更新後 ImmutableList 項目數量: {updatedProductIds.Count}");

        // 3. ImmutableArray default 陷阱
        ImmutableArray<OrderLine> items = default;
        Console.WriteLine($"items.IsDefault = {items.IsDefault}"); // True
        try
        {
            _ = items.Length;
        }
        catch (NullReferenceException)
        {
            Console.WriteLine(
                "捕獲 NullReferenceException！" +
                "未初始化的 ImmutableArray 預設底層是 null。");
        }
    }
}
