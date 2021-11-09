using CommandLine;

namespace KnogUsbUtility;

public class CommandLineOptions {
    [Option("export-to", HelpText = "Export the current configuration to the specified file.", SetName = "export")]
    public string? ExportToFileName { get; set; }

    [Option("import-from", HelpText = "Import the configuration from the specified file.", SetName = "import")]
    public string? ImportFromFileName { get; set; }

    [Option("dump-memory", HelpText = "Dump the contents of the memory to a text file (for debugging).", SetName = "dump")]
    public string? DumpMemoryToFileName { get; set; }
}
