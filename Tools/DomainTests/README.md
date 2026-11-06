# Pure C# regression tests

From the repository root, with .NET 8 SDK:

```
dotnet run --project Tools/DomainTests -- --result=Artifacts/BalanceLab/WZ1-preview-rebuilt/domain-tests.xml
dotnet run --project Tools/BalanceLab -- . Artifacts/BalanceLab/WZ1-preview-rebuilt --preview
```

NUnitLite executes selected actual engine tests plus preview/library regressions.
Unity-dependent tests are excluded explicitly, not counted as passing. Run the full
EditMode and PlayMode suites in Unity before merging. The existing golden hash is unchanged.
