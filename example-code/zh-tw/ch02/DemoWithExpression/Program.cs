Console.WriteLine("=== 示範 with 運算式 ===");

var originalLine = new OrderLine("P001", 5);
var updatedLine = originalLine with { Quantity = 10 };

Console.WriteLine("基本 with：");
Console.WriteLine($"originalLine.Quantity = {originalLine.Quantity}");
Console.WriteLine($"updatedLine.Quantity  = {updatedLine.Quantity}");

var profile = new CustomerProfile(
    "C001",
    "王小明",
    new Address("中山路 1 號", "台北市", "10001"));

var updatedProfile = profile with
{
    ShippingAddress = profile.ShippingAddress with
    {
        City = "新北市",
        PostalCode = "22001"
    }
};

Console.WriteLine();
Console.WriteLine("巢狀 with：");
PrintProfile("原始 profile", profile);
PrintProfile("更新 profile", updatedProfile);

var changedByMethod = profile.WithCity("桃園市");

Console.WriteLine();
Console.WriteLine("使用 WithCity：");
PrintProfile("原始 profile", profile);
PrintProfile("WithCity 後", changedByMethod);

static void PrintProfile(string label, CustomerProfile profile)
{
    Console.WriteLine(
        $"{label}: {profile.Name}, " +
        $"{profile.ShippingAddress.City} " +
        $"{profile.ShippingAddress.PostalCode}");
}

public record OrderLine(string ProductId, int Quantity);

public record Address(
    string Street,
    string City,
    string PostalCode
);

public record CustomerProfile(
    string Id,
    string Name,
    Address ShippingAddress)
{
    public CustomerProfile WithCity(string city)
    {
        return this with
        {
            ShippingAddress = ShippingAddress with { City = city }
        };
    }
}
