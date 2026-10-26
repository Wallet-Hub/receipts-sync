namespace Receipts.Sync.Cli.Commands
{
    /// <summary>
    /// Parsed command line of the console application.
    /// </summary>
    public sealed class CommandLineArguments
    {
        public const string Usage = """
            receipts-sync - scans the enabled file providers and records the receipt files it finds.

            Usage:
              receipts-sync [options]

            Options:
              --provider <name|id>  Scan only the given provider. Can be repeated.
              --dry-run             Scan and hash the files without writing to the database.
              -h, --help            Show this help.

            Configuration:
              ConnectionStrings:ReceiptsSync  SQLite connection string (default: Data Source=receipts-sync.db).
              RECEIPTS_SYNC_*                 Environment variables overriding appsettings.json values.
            """;

        private CommandLineArguments(bool showHelp, bool dryRun, IReadOnlyList<string> providers)
        {
            ShowHelp = showHelp;
            DryRun = dryRun;
            Providers = providers;
        }

        public bool ShowHelp { get; }

        public bool DryRun { get; }

        /// <summary>
        /// Providers requested with <c>--provider</c>, as a name or an id. Empty means every enabled provider.
        /// </summary>
        public IReadOnlyList<string> Providers { get; }

        public static CommandLineArguments Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var providers = new List<string>();
            var dryRun = false;
            var showHelp = false;

            for (var index = 0; index < args.Length; index++)
            {
                string argument = args[index];

                switch (argument)
                {
                    case "-h" or "--help":
                        showHelp = true;
                        break;

                    case "--dry-run":
                        dryRun = true;
                        break;

                    case "--provider":
                        if (index + 1 >= args.Length)
                        {
                            throw new ArgumentException("'--provider' requires a value.");
                        }

                        providers.Add(args[++index]);
                        break;

                    default:
                        if (argument.StartsWith("--provider=", StringComparison.Ordinal))
                        {
                            providers.Add(argument["--provider=".Length..]);
                            break;
                        }

                        throw new ArgumentException($"Unknown argument '{argument}'. Use --help for the available options.");
                }
            }

            return new CommandLineArguments(showHelp, dryRun, providers);
        }
    }
}
