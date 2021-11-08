using System;
using System.Collections.Generic;
using System.Linq;

namespace KnogUsbUtility {
    public class LightConfiguration {

        private const int MaxModeCount = 8;

        private const int StepOffsetOffset = 1; // 0xF801
        private const int DelayOffset = StepOffsetOffset + MaxModeCount + 1; // 0xF80A
        private const int StatusOffset = DelayOffset + MaxModeCount * 2; // 0xF81A
        private const int ButtonDataOffset = StatusOffset + MaxModeCount; // 0xF822

        private readonly static byte[] buttonData =
            new byte[] { 0x02, 0x07, 0x07, 0xff, 0xff, 0xff };
        private const byte EnabledModeStatus = 0x01;
        private const byte DisabledModeStatus = 0x07;

        public IList<Mode> Modes { get; set; } = new List<Mode>();

        private static readonly Mode disabledMode = new() {
            IsEnabled = false,
            ShortDelay = 0xFF,
            LongDelay = 0xFF,
        };

        public static int ModeDataSize => buttonData.Length + ButtonDataOffset;
        public static int StepConfigurationMaxSize => 256;

        public byte[] EncodeModeData() {
            List<byte> bytes = new();

            // pad list to MaxModeCount items
            IList<Mode> modes = Modes
                .Concat(Enumerable.Repeat(disabledMode, MaxModeCount - Modes.Count))
                .ToList();

            // Light modes offsets @ 0xF800

            bytes.Add(0); // 0xF800
            bytes.Add(0); // 0xF801

            int offset = 0;
            for (int i = 0; i < MaxModeCount; i++) {
                offset += modes[i].Steps.Count;
                bytes.Add((byte)offset);
            }

            // Delay periods @ 0xF80A
            for (int i = 0; i < MaxModeCount; i++) {
                bytes.Add(modes[i].ShortDelay);
                bytes.Add(modes[i].LongDelay);
            }

            // mode status @ 0xF81A
            for (int i = 0; i < MaxModeCount; i++) {
                bytes.Add(modes[i].IsEnabled ? EnabledModeStatus : DisabledModeStatus);
            }

            // button data @ 0xF822
            bytes.AddRange(buttonData);

            return bytes.ToArray();
        }

        public byte[] EncodeStepData() =>
            Modes
                .SelectMany(m => m.Steps)
                .Select(s => s.ConvertToByte())
                .ToArray();

        public static LightConfiguration Decode(byte[] modeData, byte[] stepData) {
            if (modeData.Length != ModeDataSize) {
                throw new ArgumentException("modeData not of correct length", nameof(modeData));
            }

            // data validity sanity check (0xF800, 0xF801)
            if (modeData[0] != 0 || modeData[1] != 0) {
                throw new ArgumentException("Invalid data");
            }

            Mode[] modes = Enumerable.Range(0, MaxModeCount)
                .Select(i => new Mode())
                .ToArray();

            for (int i = 0; i < MaxModeCount; i++) {
                int offset = i * 2 + DelayOffset;
                modes[i].ShortDelay = modeData[offset];
                modes[i].LongDelay = modeData[offset + 1];
            }

            for (int i = 0; i < MaxModeCount; i++) {
                int offset = modeData[StepOffsetOffset + i];
                int nextOffset = modeData[StepOffsetOffset + i + 1];
                modes[i].Steps = stepData[offset..nextOffset]
                    .Select(b => Step.FromByte(b))
                    .ToList();
            }

            for (int i = 0; i < MaxModeCount; i++) {
                byte b = modeData[i + StatusOffset];
                if (b == DisabledModeStatus) {
                    modes[i].IsEnabled = false;
                } else if (b != EnabledModeStatus) {
                    throw new ArgumentException("Unexpected byte");
                }
            }

            if (!modeData[ButtonDataOffset..].SequenceEqual(buttonData)) {
                throw new ArgumentException("unexpected button data");
            }

            return new LightConfiguration {
                Modes = modes.Where(m => m.IsEnabled).ToList(),
            };
        }
    }
}
