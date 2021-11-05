using System.IO;
using System.Text.Json;

namespace KnogUsbUtility {

    public static class Program {
        private static readonly string JsonFileName = "current-config.json";
        private static readonly string MemoryDumpFileName = "memory-dump.txt";

        public static void Main() =>
            DownloadAndDumpToJson();

        private static void DumpMemory() {
            using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
            using TextWriter writer = new StreamWriter(MemoryDumpFileName);
            uploader.DumpMemory(writer);
        }

        private static void UploadFromJson() {
            LightConfiguration? config =
                JsonSerializer.Deserialize<LightConfiguration>(File.ReadAllText(JsonFileName));

            if (config == null) {
                throw new InvalidDataException("Could not read " + JsonFileName);
            }

            using LightConfigurationUploader uploader = LightConfigurationUploader.Create();
            uploader.Upload(config);
        }

        private static void DownloadAndDumpToJson() {
            using LightConfigurationUploader uploader = LightConfigurationUploader.Create();

            LightConfiguration config = uploader.Download();

            string result = JsonSerializer.Serialize(config, new JsonSerializerOptions {
                WriteIndented = true,
            });

            File.WriteAllText("current-config.json", result);
        }
    }
}
