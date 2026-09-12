// readonly record struct 適合小型、不可變的值。
var price = new Money(1200m, "TWD");
var discounted = price with { Amount = 960m };

Console.WriteLine(price);      // Money { Amount = 1200, Currency = TWD }
Console.WriteLine(discounted); // Money { Amount = 960, Currency = TWD }

// 一般 record struct 的位置式屬性預設可修改。
var point = new MutablePoint(1.0, 2.0);
point.X = 5.0;
Console.WriteLine(point); // MutablePoint { X = 5, Y = 2 }

// 陣列索引子可以直接修改元素。
var array = new MutableCounter[] { new() };
array[0].Increment();
Console.WriteLine(array[0].Value); // 輸出：1

// 可變 struct 經由介面索引子取出時，只會改到複本。
IList<MutableCounter> list = new List<MutableCounter> { new() };
list[0].Increment();
Console.WriteLine(list[0].Value); // 輸出：0

public readonly record struct Money(
    decimal Amount,
    string Currency
);

public record struct MutablePoint(double X, double Y);

public struct MutableCounter
{
    public int Value;

    public void Increment()
    {
        Value++;
    }
}
