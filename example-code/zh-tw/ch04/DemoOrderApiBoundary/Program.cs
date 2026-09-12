using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<
    IOrderRepository,
    InMemoryOrderRepository>();
builder.Services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();
builder.Services.AddSingleton<
    IOrderApplicationService,
    OrderApplicationService>();

builder.Services
    .AddOptions<OrderOptions>()
    .Bind(builder.Configuration.GetSection("OrderSettings"))
    .Validate(options => options.MaxLinesPerOrder > 0,
        "MaxLinesPerOrder 必須大於零")
    .Validate(options => options.MaxOrderTotal > 0,
        "MaxOrderTotal 必須大於零")
    .ValidateOnStart();

builder.Services.AddSingleton(serviceProvider =>
    serviceProvider
        .GetRequiredService<IOptions<OrderOptions>>()
        .Value
        .ToSettings());

var app = builder.Build();

app.MapPost("/orders", async (
    CreateOrderRequest request,
    IOrderApplicationService orderService,
    CancellationToken ct) =>
{
    var order = await orderService.CreateAsync(request, ct);
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapGet("/settings", (OrderSettings settings) =>
    Results.Ok(settings));

app.Run();

public sealed record CreateOrderRequest(
    [Required, MaxLength(50)]
    string CustomerId,

    [Required]
    [MinLength(1, ErrorMessage = "至少需要一個訂單項目")]
    IReadOnlyList<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(
    [Required]
    string ProductId,

    [Range(1, int.MaxValue)]
    int Quantity);

public sealed record OrderResponse(
    string Id,
    string CustomerId,
    string Status,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    string? CancelReason);

public sealed record OrderLineResponse(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderLine(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public enum OrderStatus
{
    Draft,
    Submitted,
    Paid,
    Shipped,
    Cancelled
}

public sealed record Order(
    string Id,
    string CustomerId,
    OrderStatus Status,
    ImmutableArray<OrderLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt = null,
    DateTimeOffset? PaidAt = null,
    DateTimeOffset? ShippedAt = null,
    string? CancelReason = null)
{
    public Order Pay(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new InvalidOperationException(
                "只有已提交的訂單可以付款");
        }

        return this with
        {
            Status = OrderStatus.Paid,
            PaidAt = now
        };
    }
}

public static class OrderFactory
{
    public static Order Create(
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

        return new Order(
            id,
            customerId,
            OrderStatus.Draft,
            lines,
            lines.Sum(line => line.Quantity * line.UnitPrice),
            now);
    }
}

public static class OrderMapper
{
    public static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Lines
                .Select(line => new OrderLineResponse(
                    line.ProductId,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice,
                    line.Quantity * line.UnitPrice))
                .ToArray(),
            order.Total,
            order.CreatedAt,
            order.SubmittedAt,
            order.PaidAt,
            order.ShippedAt,
            order.CancelReason);
    }
}

public interface IOrderApplicationService
{
    Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken ct);
}

public sealed class OrderApplicationService
    : IOrderApplicationService
{
    private readonly IOrderRepository _repository;
    private readonly IProductCatalog _productCatalog;
    private readonly TimeProvider _timeProvider;

    public OrderApplicationService(
        IOrderRepository repository,
        IProductCatalog productCatalog,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _productCatalog = productCatalog;
        _timeProvider = timeProvider;
    }

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken ct)
    {
        var lines = ImmutableArray.CreateBuilder<OrderLine>(
            request.Lines.Count);

        foreach (var line in request.Lines)
        {
            var product = await _productCatalog.GetAsync(
                line.ProductId, ct)
                ?? throw new InvalidOperationException(
                    $"找不到商品：{line.ProductId}");

            lines.Add(new OrderLine(
                line.ProductId,
                product.Name,
                line.Quantity,
                product.UnitPrice));
        }

        var order = OrderFactory.Create(
            Guid.NewGuid().ToString("N"),
            request.CustomerId,
            lines.ToImmutable(),
            _timeProvider.GetUtcNow());

        await _repository.SaveAsync(order, ct);
        return OrderMapper.ToResponse(order);
    }
}

public interface IOrderRepository
{
    Task<Order?> GetAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default);
}

public sealed record PayOrderCommand(string OrderId);

public sealed record CancelOrderCommand(
    string OrderId,
    string Reason,
    string CancelledBy);

public sealed record OrderPaidEvent(
    string OrderId,
    string CustomerId,
    decimal Amount,
    DateTimeOffset PaidAt);

public sealed record OrderShippedEvent(
    string OrderId,
    string TrackingNumber,
    DateTimeOffset ShippedAt);

public sealed class PayOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PayOrderCommandHandler(
        IOrderRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(
        PayOrderCommand command,
        CancellationToken ct)
    {
        var order = await _repository.GetAsync(command.OrderId, ct)
            ?? throw new KeyNotFoundException(
                $"找不到訂單：{command.OrderId}");

        var paidAt = _timeProvider.GetUtcNow();
        var paid = order.Pay(paidAt);
        await _repository.SaveAsync(paid, ct);
    }
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Task<Order?> GetAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task SaveAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}

public sealed record Product(
    string Id,
    string Name,
    decimal UnitPrice);

public interface IProductCatalog
{
    Task<Product?> GetAsync(
        string productId,
        CancellationToken cancellationToken);
}

public sealed class InMemoryProductCatalog : IProductCatalog
{
    private static readonly IReadOnlyDictionary<
        string, Product> Products =
        new Dictionary<string, Product>
        {
            ["P001"] = new("P001", "機械式鍵盤", 2_800m),
            ["P002"] = new("P002", "USB-C 傳輸線", 450m)
        };

    public Task<Product?> GetAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }
}

public sealed class OrderOptions
{
    public int MaxLinesPerOrder { get; set; }
    public decimal MaxOrderTotal { get; set; }
    public int CancellationWindowHours { get; set; }

    public OrderSettings ToSettings() =>
        new(
            MaxLinesPerOrder,
            MaxOrderTotal,
            CancellationWindowHours);
}

public sealed record OrderSettings(
    int MaxLinesPerOrder,
    decimal MaxOrderTotal,
    int CancellationWindowHours);

public sealed record ValidatedOrderInput
{
    public string CustomerId { get; }
    public ImmutableArray<CreateOrderLineRequest> Lines { get; }

    private ValidatedOrderInput(
        string customerId,
        ImmutableArray<CreateOrderLineRequest> lines)
    {
        CustomerId = customerId;
        Lines = lines;
    }

    public static Result<ValidatedOrderInput> Create(
        string? customerId,
        IReadOnlyList<CreateOrderLineRequest>? lines)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(customerId))
            errors.Add("CustomerId 是必填欄位");

        if (lines is null || lines.Count == 0)
            errors.Add("至少需要一個訂單項目");

        if (lines?.Any(line => line.Quantity <= 0) == true)
            errors.Add("訂單數量必須大於零");

        if (errors.Count > 0)
            return Result<ValidatedOrderInput>.Fail(errors);

        return Result<ValidatedOrderInput>.Success(
            new ValidatedOrderInput(
                customerId!,
                [.. lines!]));
    }
}

public sealed record Result<T>(
    T? Value,
    ImmutableArray<string> Errors)
    where T : class
{
    public bool IsSuccess => Value is not null;

    public static Result<T> Success(T value) =>
        new(value, []);

    public static Result<T> Fail(IEnumerable<string> errors) =>
        new(null, [.. errors]);
}
