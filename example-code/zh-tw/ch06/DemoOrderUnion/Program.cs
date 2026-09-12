using System;
using System.Collections.Immutable;
using System.Linq;

var lines = ImmutableArray.Create(
    new OrderLine("P001", "機械式鍵盤", 1, 2_800m),
    new OrderLine("P002", "USB-C 傳輸線", 2, 450m));

var createdAt = new DateTimeOffset(
    2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

var draft = OrderFactory.Create(
    "ORD-001", "CUST-42", lines, createdAt);
var submitted = OrderWorkflow.Submit(
    draft, createdAt.AddMinutes(5));
var paid = OrderWorkflow.Pay(
    submitted, createdAt.AddHours(2));
var shipped = OrderWorkflow.Ship(
    paid, "TRK-20260101", createdAt.AddDays(1));

Order order = shipped;
Console.WriteLine(OrderQueries.GetStatusDescription(order));

public sealed record OrderLine(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record DraftOrder(
    string Id,
    string CustomerId,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt);

public sealed record SubmittedOrder(
    string Id,
    string CustomerId,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset SubmittedAt);

public sealed record PaidOrder(
    string Id,
    string CustomerId,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset SubmittedAt,
    DateTimeOffset PaidAt);

public sealed record ShippedOrder(
    string Id,
    string CustomerId,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset SubmittedAt,
    DateTimeOffset PaidAt,
    DateTimeOffset ShippedAt,
    string TrackingNumber);

public sealed record CancelledOrder(
    string Id,
    string CustomerId,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    string CancelReason);

public union Order(
    DraftOrder,
    SubmittedOrder,
    PaidOrder,
    ShippedOrder,
    CancelledOrder);

public static class OrderFactory
{
    public static DraftOrder Create(
        string id,
        string customerId,
        ImmutableArray<OrderLine> lines,
        DateTimeOffset now)
    {
        if (lines.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "至少需要一個訂單項目",
                nameof(lines));
        }

        return new DraftOrder(
            id,
            customerId,
            lines,
            lines.Sum(line => line.Quantity * line.UnitPrice),
            now);
    }
}

public static class OrderWorkflow
{
    public static SubmittedOrder Submit(
        DraftOrder draft,
        DateTimeOffset now) =>
        new(
            draft.Id,
            draft.CustomerId,
            draft.Lines,
            draft.Total,
            draft.CreatedAt,
            SubmittedAt: now);

    public static PaidOrder Pay(
        SubmittedOrder submitted,
        DateTimeOffset now) =>
        new(
            submitted.Id,
            submitted.CustomerId,
            submitted.Lines,
            submitted.Total,
            submitted.CreatedAt,
            submitted.SubmittedAt,
            PaidAt: now);

    public static ShippedOrder Ship(
        PaidOrder paid,
        string trackingNumber,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new ArgumentException(
                "出貨時必須提供追蹤號碼",
                nameof(trackingNumber));
        }

        return new ShippedOrder(
            paid.Id,
            paid.CustomerId,
            paid.Lines,
            paid.Total,
            paid.CreatedAt,
            paid.SubmittedAt,
            paid.PaidAt,
            ShippedAt: now,
            TrackingNumber: trackingNumber);
    }

    public static CancelledOrder Cancel(
        DraftOrder order,
        string reason) =>
        CancelCore(
            order.Id,
            order.CustomerId,
            order.Lines,
            order.Total,
            order.CreatedAt,
            reason);

    public static CancelledOrder Cancel(
        SubmittedOrder order,
        string reason) =>
        CancelCore(
            order.Id,
            order.CustomerId,
            order.Lines,
            order.Total,
            order.CreatedAt,
            reason);

    public static CancelledOrder Cancel(
        PaidOrder order,
        string reason) =>
        CancelCore(
            order.Id,
            order.CustomerId,
            order.Lines,
            order.Total,
            order.CreatedAt,
            reason);

    private static CancelledOrder CancelCore(
        string id,
        string customerId,
        ImmutableArray<OrderLine> lines,
        decimal total,
        DateTimeOffset createdAt,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "取消訂單必須提供原因",
                nameof(reason));
        }

        return new CancelledOrder(
            id,
            customerId,
            lines,
            total,
            createdAt,
            CancelReason: reason);
    }
}

public static class OrderQueries
{
    public static string GetStatusDescription(Order order) =>
        order switch
        {
            DraftOrder => "草稿",
            SubmittedOrder => "已提交",
            PaidOrder => "已付款",
            ShippedOrder shipped =>
                $"已出貨，追蹤號碼：{shipped.TrackingNumber}",
            CancelledOrder cancelled =>
                $"已取消：{cancelled.CancelReason}"
        };
}
