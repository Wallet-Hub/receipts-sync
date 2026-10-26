# Receipts Sync

Console application that reads the enabled `FileProvider` rows from a SQLite database, scans the files
stored on them and records the scanned files (plus their metadata: hash, size, content type, timestamps)
into `ExpenseFileReceipt`.

The goal of this first iteration is only to **populate `ExpenseFileReceipt`**: no OCR, no extraction,
no deletion handling. Files removed from a provider stay in the database.

## Projects

| Project | Purpose |
| --- | --- |
| `Receipts.Sync.Data` | Entities (`FileProvider`, `ExpenseFileReceipt`, `ExpenseReceiptExtraction`). |
| `Receipts.Sync.Data.EfCore` | `ExpenseReceiptDbContext` and entity configurations (database agnostic). |
| `Receipts.Sync.Data.EfCore.Sqlite` | SQLite provider, EF Core migrations and `AddExpenseReceiptsSqlite`. |
| `Receipts.Sync.Providers` | Provider abstraction (`IFileProviderSource`), registry, SHA-256 hasher and the `PhysicalFolder` source. |
| `Receipts.Sync.Providers.GoogleDrive` | Google Drive source (optional package: remove the registration to drop the dependency). |
| `Receipts.Sync.Core` | `ReceiptSyncService`: scans the providers and upserts `ExpenseFileReceipt` rows. |
| `Receipts.Sync.Cli` | Console entry point (hosting, configuration, migrations, summary). |
| `Receipts.Sync.Tests` | xUnit tests (providers, hashing, registry, synchronization). |

## Requirements

- .NET SDK 10
- The local tool `dotnet-ef` (pinned in `.config/dotnet-tools.json`): run `dotnet tool restore`

## Build and test

```powershell
dotnet tool restore
dotnet build Receipts.Sync.slnx
dotnet test Receipts.Sync.Tests
```

## Usage

```powershell
dotnet run --project Receipts.Sync.Cli -- --help
dotnet run --project Receipts.Sync.Cli -- --provider local-receipts
dotnet run --project Receipts.Sync.Cli -- --provider 1 --provider 3 --dry-run
```

Options:

| Option | Description |
| --- | --- |
| `--provider <name or id>` | Scans only the given provider (repeatable). Without it every enabled provider is scanned. |
| `--dry-run` | Scans the providers without writing anything to the database. |
| `-h`, `--help` | Shows the usage. |

The application applies the EF Core migrations at startup, so the first run creates the database.

Configuration (`appsettings.json` in the working directory or `RECEIPTS_SYNC_*` environment variables):

```json
{
  "ConnectionStrings": {
    "ReceiptsSync": "Data Source=receipts-sync.db"
  }
}
```

```powershell
$env:RECEIPTS_SYNC_ConnectionStrings__ReceiptsSync = "Data Source=C:\data\receipts.db"
```

Exit codes: `0` success, `1` at least one provider failed, `2` invalid arguments or unknown provider,
`130` cancelled.

### Debugging

`Receipts.Sync.Cli/Properties/launchSettings.json` contains ready-made profiles (full scan, dry run, SQL
logging, single provider, development database, `--help`). Select one in Visual Studio or Rider, or run it
from the terminal:

```powershell
dotnet run --project Receipts.Sync.Cli --launch-profile "Scansione con log SQL"
```

Things to keep in mind while debugging:

- `dotnet run` ignores the `workingDirectory` of a launch profile, so `appsettings.json` and relative
  `Data Source=` paths resolve against the directory the command is started from (the repository root).
- Configuration keys are case-sensitive: after the `RECEIPTS_SYNC_` prefix the variable must keep the exact
  casing of the setting, e.g. `RECEIPTS_SYNC_ConnectionStrings__ReceiptsSync`. An all-uppercase name like
  `RECEIPTS_SYNC_CONNECTIONSTRINGS__RECEIPTSYNC` is silently ignored.

## Registering a provider

Providers are plain rows in `FileProviders`. `Configuration` holds the provider specific settings as
JSON, `ProviderType` selects the implementation:

```sql
INSERT INTO FileProviders (Name, ProviderType, Configuration, IsEnabled, CreatedAt, UpdatedAt)
VALUES ('local-receipts', 'PhysicalFolder',
        '{"rootPath":"D:\\Receipts","recursive":true,"extensions":[".pdf",".jpg"]}',
        1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
```

`(FileProviderId, ExternalId)` is unique, so running the synchronization twice does not duplicate rows.
`ExternalId` is the relative path with `/` separators for local folders and the Drive file id for
Google Drive.

### PhysicalFolder

```json
{
  "rootPath": "D:\\Receipts",
  "recursive": true,
  "extensions": [".pdf", ".jpg"]
}
```

`rootPath` supports environment variables (`%USERPROFILE%\\Receipts`). `ExternalId` is the path relative
to `rootPath`; paths resolving outside `rootPath` are rejected.

### GoogleDrive

```json
{
  "credentialsPath": "C:\\secrets\\google-service-account.json",
  "impersonateUser": "receipts@example.com",
  "folderId": "1AbCdEfGhIjKlMnOpQrStUvWxYz",
  "recursive": true,
  "includeSharedDrives": true,
  "extensions": [".pdf"]
}
```

- Provide the credentials with `credentialsPath` **or** `credentialsJsonEnvironmentVariable`: the name
  of an environment variable holding the service account JSON. Credentials must never be stored in the
  database.
- `folderId` scans that folder (recursively by default); `query` takes a raw Drive query instead
  (`name contains 'receipt' and trashed = false`); with neither, every file visible to the credentials
  is scanned.
- Google Workspace documents (Docs, Sheets, ...) and folders are skipped because they have no binary
  content to hash.

## Adding a new provider

1. Create a project (e.g. `Receipts.Sync.Providers.AzureBlobStorage`) referencing `Receipts.Sync.Providers`.
2. Implement `IFileProviderSource`: `ProviderType`, `EnumerateFilesAsync` (metadata only) and
   `OpenReadAsync` (content used for hashing). Both receive a `ProviderContext` (id, name, type and
   raw JSON configuration) instead of the `FileProvider` entity, so sources never depend on the
   persistence model; `Receipts.Sync.Core` maps the row with `ToProviderContext()`.
3. Add a `ServiceCollectionExtensions.AddAzureBlobFileProvider()` that registers the source with
   `services.AddFileProviderSource<AzureBlobFileProviderSource>()`.
4. Call it from `Program.cs`. No change is required in `Receipts.Sync.Core`: the service resolves the
   source through `IFileProviderSourceRegistry` using `FileProviderType`.

## How the synchronization works

For every enabled provider the service enumerates the files and upserts one `ExpenseFileReceipt` per
`ExternalId`:

- **new file** -> row inserted, content streamed through SHA-256 (`HashAlgorithm = "SHA256"`);
- **known file** -> the hash is reused when the provider reports the same size and the same modification
  timestamp, otherwise the file is hashed again; the row is updated only when the hash or another
  metadata changed;
- rows are saved in batches of 100 and a provider failure is reported without stopping the others.

Hashing lives in `Sha256FileHasher`, so switching algorithm means replacing `IFileHasher`.
