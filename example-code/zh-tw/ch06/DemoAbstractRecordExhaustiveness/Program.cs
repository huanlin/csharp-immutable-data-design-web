System.Console.WriteLine(Demo.Describe(new DraftOrder()));

public abstract record Order;

public sealed record DraftOrder : Order;

public sealed record SubmittedOrder : Order;

public sealed record PaidOrder : Order;

public sealed record ShippedOrder : Order;

public sealed record CancelledOrder : Order;

public static class Demo
{
    public static string Describe(Order order) => order switch
    {
        DraftOrder => "draft",
        SubmittedOrder => "submitted",
        PaidOrder => "paid",
        ShippedOrder => "shipped",
        CancelledOrder => "cancelled",
    };
}
