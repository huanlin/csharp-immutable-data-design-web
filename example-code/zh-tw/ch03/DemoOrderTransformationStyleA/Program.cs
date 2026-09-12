using System;
using System.Collections.Immutable;

namespace DemoOrderTransformationStyleA;

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
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? SubmittedAt { get; private init; }
    public DateTimeOffset? PaidAt { get; private init; }
    public DateTimeOffset? ShippedAt { get; private init; }
    public string? CancelReason { get; private init; }

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

    public Order Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException(
                "只有已付款的訂單可以出貨");

        return this with
        {
            Status = OrderStatus.Shipped,
            ShippedAt = now
        };
    }

    public Order Cancel(string reason)
    {
        if (Status is not (
            OrderStatus.Draft or
            OrderStatus.Submitted or
            OrderStatus.Paid))
            throw new InvalidOperationException(
                "只有草稿、已提交、或已付款的訂單可以取消");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "取消訂單必須提供原因",
                nameof(reason));

        return this with
        {
            Status = OrderStatus.Cancelled,
            CancelReason = reason
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

        Console.WriteLine($"初始狀態：{draft.Status}");

        var submitted = draft.Submit(now);
        Console.WriteLine($"提交後狀態：{submitted.Status}");

        var paid = submitted.Pay(now.AddMinutes(5));
        Console.WriteLine($"付款後狀態：{paid.Status}");

        var shipped = paid.Ship(now.AddHours(2));
        Console.WriteLine($"出貨後狀態：{shipped.Status}");
    }
}
