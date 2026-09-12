using System;
using System.Collections.Immutable;

namespace DemoOrderTransformationPipeline;

public enum OrderStatus
{
    Draft,
    Submitted,
    Paid,
    Shipped,
    Cancelled
}

public sealed record OrderLine(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;
}

public sealed record Order
{
    public string Id { get; private init; }
    public string CustomerId { get; private init; }
    public OrderStatus Status { get; private init; }
    public ImmutableArray<OrderLine> Lines { get; private init; }
    public decimal Total { get; private init; }
    public decimal DiscountRate { get; private init; }
    public decimal ShippingFee { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? SubmittedAt { get; private init; }
    public DateTimeOffset? PaidAt { get; private init; }

    private Order(
        string id,
        string customerId,
        ImmutableArray<OrderLine> lines,
        DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        Lines = lines;
        Total = lines.Sum(line => line.Amount);
        DiscountRate = 1m;
        ShippingFee = 0m;
        CreatedAt = createdAt;
    }

    public static Order CreateDraft(
        string id,
        string customerId,
        ImmutableArray<OrderLine> lines,
        DateTimeOffset createdAt)
    {
        if (lines.IsDefault)
        {
            throw new ArgumentException(
                "訂單項目尚未初始化",
                nameof(lines));
        }

        return new Order(id, customerId, lines, createdAt);
    }

    // 計算最終金額
    public decimal FinalTotal => (Total * DiscountRate) + ShippingFee;

    public Order ApplyDiscount(decimal rate)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("只有草稿可以套用折扣");
        if (rate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(rate));

        return this with { DiscountRate = rate };
    }

    public Order ApplyShippingFee(decimal fee)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("只有草稿可以計算運費");
        if (fee < 0m)
            throw new ArgumentOutOfRangeException(nameof(fee));

        return this with { ShippingFee = fee };
    }

    public Order Submit(DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("只有草稿可以提交");
        if (Lines.IsEmpty)
            throw new InvalidOperationException(
                "訂單必須至少有一個項目才能提交");

        return this with
        {
            Status = OrderStatus.Submitted,
            SubmittedAt = now
        };
    }

    public Order Pay(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted)
            throw new InvalidOperationException(
                "只有已提交的訂單可以付款");

        return this with
        {
            Status = OrderStatus.Paid,
            PaidAt = now
        };
    }
}

public class Program
{
    public static void Main()
    {
        var timeProvider = TimeProvider.System;
        var now = timeProvider.GetUtcNow();

        var lines = ImmutableArray.Create(
            new OrderLine(
                "P001",
                "C# 不可變資料設計入門",
                1,
                500m));

        var draft = Order.CreateDraft(
            "ORD-001",
            "CUST-42",
            lines,
            now);

        // 轉換管線：鏈式呼叫
        var processedOrder = draft
            .ApplyDiscount(0.9m)       // 9 折折扣
            .ApplyShippingFee(60m)     // 運費 60 元
            .Submit(now)               // 提交
            .Pay(now.AddMinutes(5));   // 付款

        Console.WriteLine($"原始訂單總金額：{draft.FinalTotal}");
        Console.WriteLine($"處理後訂單狀態：{processedOrder.Status}");
        Console.WriteLine($"處理後折扣率：{processedOrder.DiscountRate}");
        Console.WriteLine($"處理後運費：{processedOrder.ShippingFee}");
        Console.WriteLine($"處理後最終金額：{processedOrder.FinalTotal}");
    }
}
