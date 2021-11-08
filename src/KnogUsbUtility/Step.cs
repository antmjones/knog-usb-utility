using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnogUsbUtility {
    [JsonConverter(typeof(Step.StepJsonConverter))]
    public class Step {
        public const int MinBrightness = 0;
        public const int MaxBrightness = 7;

        private const int BrightnessShift = 5;
        private const int UseLongDelayBit = 1 << 4;
        private const int Channel1Bit = 1 << 3;
        private const int Channel2Bit = 1 << 2;
        private const int Channel3Bit = 1 << 1;

        private int brightness;

        public int Brightness {
            get => brightness;
            set {
                if (value is < MinBrightness or > MaxBrightness) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                brightness = value;
            }
        }

        public bool UseLongDelay { get; set; }

        public bool Channel1 { get; set; }
        public bool Channel2 { get; set; }
        public bool Channel3 { get; set; }

        public static Step FromByte(byte b) {
            bool IsSet(byte mask) => (b & mask) != 0;

            return new Step {
                Brightness = b >> BrightnessShift,
                UseLongDelay = IsSet(UseLongDelayBit),
                Channel1 = IsSet(Channel1Bit),
                Channel2 = IsSet(Channel2Bit),
                Channel3 = IsSet(Channel3Bit),
            };
        }

        public byte ConvertToByte() =>
            (byte)(Brightness << BrightnessShift |
                (UseLongDelay ? UseLongDelayBit : 0) |
                (Channel1 ? Channel1Bit : 0) |
                (Channel2 ? Channel2Bit : 0) |
                (Channel3 ? Channel3Bit : 0));

        // E.g. "7 L -O-"
        public static Step FromPattern(string str) {
            if (str.Length != 7 ||
                str[1] != ' ' ||
                str[3] != ' ') {
                throw new ArgumentException("Invalid pattern", nameof(str));
            }

            return new Step {
                Brightness = int.Parse(str.Substring(0, 1),
                    NumberStyles.None, CultureInfo.InvariantCulture),
                UseLongDelay = ParseUseLongDelay(str[2]),
                Channel1 = ParseChannel(str[4]),
                Channel2 = ParseChannel(str[5]),
                Channel3 = ParseChannel(str[6]),
            };
        }

        private static bool ParseUseLongDelay(char delay) => delay switch {
            'L' => true,
            'S' => false,
            _ => throw new ArgumentOutOfRangeException(nameof(delay)),
        };

        private static bool ParseChannel(char channel) => channel switch {
            'O' => true,
            '-' => false,
            _ => throw new ArgumentOutOfRangeException(nameof(channel)),
        };

        public override string ToString() {
            static char ChannelToChar(bool channel) => channel ? 'O' : '-';

            return
                Brightness + " " +
                (UseLongDelay ? "L" : "S") + " " +
                ChannelToChar(Channel1) +
                ChannelToChar(Channel2) +
                ChannelToChar(Channel3);
        }

        private class StepJsonConverter : JsonConverter<Step> {
            public override Step? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                Step.FromPattern(reader.GetString()!);

            public override void Write(Utf8JsonWriter writer, Step value, JsonSerializerOptions options) =>
                writer.WriteStringValue(value.ToString());
        }
    }
}
