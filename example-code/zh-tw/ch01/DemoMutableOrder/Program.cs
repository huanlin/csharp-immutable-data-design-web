using System;
using System.Collections.Generic;

Console.WriteLine("=== 示範可變狀態 (Mutable State) 的副作用 ===");

var order = new Order
{
    Id = "ORD-001",
    CustomerId = "CUST-42",
    Status = OrderStatus.Draft,
    Lines = new List<OrderLine>
    {
        new() { ItemName = "C# 書籍", UnitPrice = 500, Quantity = 2 }
    },
    Total = 1000,
    CreatedAt = DateTimeOffset.UtcNow
};

var service = new OrderService();

Console.WriteLine(
    $"付款前狀態: {order.Status}, 付款時間: {order.PaidAt}");

// 呼叫 Pay 方法，這會原地修改 order 物件的狀態
service.Pay(order, DateTimeOffset.UtcNow);

Console.WriteLine(
    $"付款後狀態: {order.Status}, 付款時間: {order.PaidAt}");

// 示範隱憂二：任何地方都可以任意修改
order.Status = OrderStatus.Cancelled;
order.PaidAt = null;
Console.WriteLine(
    $"被外部修改後狀態: {order.Status}");

public enum OrderStatus
{
    Draft,
    Submitted,
    Paid,
    Shipped,
    Cancelled
}

public class OrderLine
{
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public string? CancelReason { get; set; }
}

public class OrderService
{
    public void Submit(Order order, DateTimeOffset now)
    {
        order.Status = OrderStatus.Submitted;
        order.SubmittedAt = now;
    }

    public void Pay(Order order, DateTimeOffset now)
    {
        order.Status = OrderStatus.Paid;
        order.PaidAt = now;
    }

    public void Ship(Order order, DateTimeOffset now)
    {
        order.Status = OrderStatus.Shipped;
        order.ShippedAt = now;
    }

    public void Cancel(Order order, string reason)
    {
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = reason;
        order.SubmittedAt = null;
        order.PaidAt = null;
    }
}
