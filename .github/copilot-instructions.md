# Copilot instructions — Receipts Sync

## What this repository is

A .NET 10 console application that scans the enabled `FileProvider` rows of a SQLite database and keeps
`ExpenseFileReceipt` in sync with the files it finds. A short project map and the usage of the CLI are in
`README.md`; read it before changing behavior.

## Build and test

```powershell
dotnet tool restore
dotnet build Receipts.Sync.slnx
dotnet test Receipts.Sync.Tests
```

Run the smallest command that covers the change; use the full test suite when the change touches `Receipts.Sync.Core`.

## C# style

- `Nullable` and `ImplicitUsings` are enabled in every project; keep nullability annotations accurate.
- Block-scoped namespaces, 4-space indentation, CRLF line endings, braces on their own line.
- Prefer `sealed` classes unless the type is meant for inheritance.
- Async methods end with `Async`; library code uses `.ConfigureAwait(false)` (the CLI entry point does not need it).
- Code, comments and XML documentation are in English. Public API keeps its XML documentation.
- The `.editorconfig` intentionally disables Style analyzers, so these rules are enforced by review, not by the build.

## `var` policy (mandatory)

**Use the explicit type by default.** `var` is allowed only when the exact type is obvious at a glance from the
declaration line itself.

Use `var` only for:

- Object or collection creation: `var options = new SyncOptions();`, `var ids = new List<int>();`
- Casts and pattern matching: `var file = (FileInfo)entry;`, `if (row is FileProvider provider)`
- Simple literals whose type is unmistakable: `var dryRun = false;`, `var pendingChanges = 0;`
- Loop variables, where the type comes from the sequence being iterated: `foreach (var provider in providers)`, `for (var index = 0; index < args.Length; index++)`
- `using`/`await using` declarations initialized with `new`, because the type is written on the line.

Write the type explicitly for:

- Anything returned by a method, even when the name suggests the type: `List<FileProvider> providers = await GetProvidersAsync(...)`.
- LINQ chains, `Task` results and `Dictionary`/`IQueryable` locals.
- `out var` when the reader would have to look up the method signature: write `out IFileProviderSource? source` instead.
- Any case where a reviewer cannot name the exact type from that line alone.

Rule of thumb: **if you would need to hover the variable or open another file to know the type, do not use `var`.**

```csharp
// No
var providers = await GetProvidersAsync(options, cancellationToken).ConfigureAwait(false);
var knownReceipts = await _db.ExpenseFileReceipts.ToDictionaryAsync(...).ConfigureAwait(false);
var hash = await ComputeHashAsync(source, provider, file, cancellationToken).ConfigureAwait(false);
await using var content = await source.OpenReadAsync(provider, file, cancellationToken).ConfigureAwait(false);

// Yes
List<FileProvider> providers = await GetProvidersAsync(options, cancellationToken).ConfigureAwait(false);
Dictionary<string, ExpenseFileReceipt> knownReceipts = await _db.ExpenseFileReceipts.ToDictionaryAsync(...).ConfigureAwait(false);
string hash = await ComputeHashAsync(source, provider, file, cancellationToken).ConfigureAwait(false);
await using Stream content = await source.OpenReadAsync(provider, file, cancellationToken).ConfigureAwait(false);
```

When editing a file, convert the existing `var` declarations you touch to explicit types; leave the rest of the file alone.
