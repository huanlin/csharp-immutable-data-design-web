# DemoAbstractRecordExhaustiveness

This project verifies that an `abstract record` base type with sealed derived
record types is not treated as a closed hierarchy by the C# compiler.

Run:

```powershell
dotnet build .\DemoAbstractRecordExhaustiveness.csproj
```

Expected result: the build succeeds, but the compiler emits `CS8509` for the
`switch` expression in `Program.cs`.
