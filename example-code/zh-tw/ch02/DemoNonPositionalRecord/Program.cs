Console.WriteLine("=== 示範非位置式 Record（Non-Positional Record）===");

var line = new ValidatedOrderLine(
    productId: "P001",
    productName: "C# 不可變資料設計",
    quantity: 2,
    unitPrice: 580m);

Console.WriteLine($"商品: {line.ProductName}");
Console.WriteLine($"小計: {line.LineTotal:C}");

public sealed record ValidatedOrderLine
{
    public string ProductId { get; private init; }
    public string ProductName { get; private init; }
    public int Quantity { get; private init; }
    public decimal UnitPrice { get; private init; }
    public decimal LineTotal => Quantity * UnitPrice;

    public ValidatedOrderLine(
        string productId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException(
                "ProductId 不能為空", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity), "數量必須大於 0");

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice), "單價不能為負數");

        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
