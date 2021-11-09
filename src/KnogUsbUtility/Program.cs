using System;
using System.IO;
using System.Text.Json;
using CommandLine;

namespace KnogUsbUtility;

public static class Program {
    public static int Main(string[] args) =>
        Parser.Default
            .ParseArguments<CommandLineOptions>(args)
            .MapResult(Run, _ => 1);

    private static int Run(CommandLineOptions options) {
        try {
            if (!string.IsNullOrEmpty(options.DumpMemoryToFileName)) {
                DumpMemory(options.DumpMemoryToFileName);
            } else if (!string.IsNullOrEmpty(options.ExportToFileName)) {
                ExportToJson(options.ExportToFileName);
            } else if (!string.IsNullOrEmpty(options.ImportFromFileName)) {
                ImportFromJson(options.ImportFromFileName);
            }

            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine(ex);
            return -1;
        }
    }

    private static void DumpMemory(string toFileName) {
        using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
        using TextWriter writer = new StreamWriter(toFileName);
        uploader.DumpMemory(writer);

        Console.Write("Memory dumped to: " + toFileName);
    }

    private static void ImportFromJson(string fromFileName) {
        LightConfiguration? config =
            JsonSerializer.Deserialize<LightConfiguration>(File.ReadAllText(fromFileName));

        if (config == null) {
            throw new InvalidDataException("Could not read " + fromFileName);
        }

        using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
        uploader.Upload(config);

        Console.Write("Configuration imported from: " + fromFileName);
    }

    private static void ExportToJson(string toFileName) {
        using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
        LightConfiguration config = uploader.Download();

        string result = JsonSerializer.Serialize(config, new JsonSerializerOptions {
            WriteIndented = true,
        });

        File.WriteAllText(toFileName, result);

        Console.Write("Configuration exported to: " + toFileName);
    }
}
