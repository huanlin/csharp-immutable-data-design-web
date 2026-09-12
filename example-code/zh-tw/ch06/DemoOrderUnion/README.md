# DemoOrderUnion

This project demonstrates a type-safe order workflow using C# 15 union types.
Each order state is an independent record, and the `Order` union defines the
closed set of valid states. Workflow methods accept a specific source state
and return a specific destination state, while pattern matching over `Order`
is exhaustive.

Requirements:

- .NET 11 Preview 7 or later
- C# language version set to `preview`

Run:

```powershell
dotnet run --project .\DemoOrderUnion.csproj
```
