using System;
using System.IO;
using System.Text.Json;
using CommandLine;

namespace KnogUsbUtility;

public static class Program {
    private static readonly JsonSerializerOptions readJsonOptions =
        new JsonSerializerOptions {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

    private static readonly JsonSerializerOptions writeJsonOptions =
        new JsonSerializerOptions {
            WriteIndented = true,
        };


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
#pragma warning disable CA1031 // Do not catch general exception types
        } catch (Exception ex) {
#pragma warning restore CA1031 // Do not catch general exception types
            Console.Error.WriteLine(ex);
            return -1;
        }
    }

    private static void DumpMemory(string toFileName) {
        using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
        using TextWriter writer = new StreamWriter(toFileName);

        for (int i = LightConfigurationUploader.LightModesStartAddress;
            i <= LightConfigurationUploader.EndAddressForDump; i++) {
            byte b = uploader.ReadByte(i);
            writer.WriteLine($"0x{i:X4}: 0x{b:X2} ({b})");
        }

        Console.Write("Memory dumped to: " + toFileName);
    }

    private static void ImportFromJson(string fromFileName) {
        LightConfiguration? config =
            JsonSerializer.Deserialize<LightConfiguration>(
                File.ReadAllText(fromFileName),
                readJsonOptions);

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

        string result = JsonSerializer.Serialize(config, writeJsonOptions);

        File.WriteAllText(toFileName, result);

        Console.Write("Configuration exported to: " + toFileName);
    }
}
