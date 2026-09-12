using System.Collections.Frozen;

// C# 15 可以在 collection expression 的第一個元素使用 with(...)
// 將 comparer 等建構參數傳給底層集合。
HashSet<string> categoryCodes =
[
    with(StringComparer.OrdinalIgnoreCase),
    "BOOK",
    "SOFTWARE"
];

Console.WriteLine(categoryCodes.Contains("book")); // 輸出：True

// 真正的查詢資料仍可在建構階段使用可變集合，完成後再 freeze。
var categoryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["BOOK"] = "書籍",
    ["SOFTWARE"] = "軟體"
}.ToFrozenDictionary();

Console.WriteLine(categoryNames["book"]); // 輸出：書籍
