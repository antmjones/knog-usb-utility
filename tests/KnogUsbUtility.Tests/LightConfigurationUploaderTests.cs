using System;
using KnogUsbUtility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass]
    public class LightConfigurationUploaderTests {
        [TestMethod]
        public void TestUpload() {
            MockHidDevice mockHidDevice = new();

            mockHidDevice.ExpectWrite(new byte[] {
                0x00, 0x24, 0x04, 0x31, 0xa5, 0xf1, 0x01
            });

            mockHidDevice.ExpectWrite(new byte[] {
                0x00, 0x24, 0x2b, 0x32, 0xf8, 0x00, 0x00, 0x00,
                0x05, 0x06, 0x0a, 0x16, 0x16, 0x16, 0x16, 0x16,
                0x2f, 0x50, 0x64, 0xff, 0x0d, 0x64, 0x07, 0x3c,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0x01, 0x01, 0x01, 0x01, 0x07, 0x07, 0x07, 0x07,
                0x02, 0x07, 0x07, 0xff, 0xff, 0xff
            });

            mockHidDevice.ExpectWrite(new byte[] {
                0x00, 0x24, 0x19, 0x32, 0xf8, 0x40, 0x3e, 0x3e,
                0x3e, 0x3e, 0xee, 0x3e, 0x1e, 0x34, 0x1e, 0x3a,
                0x28, 0x1e, 0x1e, 0x24, 0x1e, 0x1e, 0x22, 0x1e,
                0x1e, 0x24, 0x1e, 0x1e
            });

            mockHidDevice.ReturnRead(new byte[] { 0x00, 0x40 });
            mockHidDevice.ReturnRead(new byte[] { 0x00, 0x40 });
            mockHidDevice.ReturnRead(new byte[] { 0x00, 0x40 });

            LightConfigurationUploader? configUploader = new(mockHidDevice, true);

            configUploader.Upload(
                LightConfiguration.Decode(LightConfigurationTests.ModeData, LightConfigurationTests.StepData));
        }

        [TestMethod]
        public void TestDownload() {
            MockHidDevice mockHidDevice = new();

            void Expect(int addr, byte expected) {
                mockHidDevice.ExpectWrite(new byte[] {
                    0x00,
                    0x24,
                    0x04,
                    0x38,
                    (byte)((addr >> 8) & 0xFF),
                    (byte)(addr & 0xFF),
                    0x01,
                });

                mockHidDevice.ReturnRead(new byte[] { 0x00, expected });
            }

            byte[] modeData = LightConfigurationTests.ModeData;

            for (int i = 0; i < modeData.Length; i++) {
                Expect(
                    LightConfigurationUploader.LightModesStartAddress + i,
                    modeData[i]);
            }

            byte[] stepData = LightConfigurationTests.StepData;

            for (int i = 0; i < LightConfiguration.StepConfigurationMaxSize; i++) {
                Expect(
                    LightConfigurationUploader.StepDataStartAddress + i,
                    i >= stepData.Length ? (byte)0xFF : stepData[i]);
            }


            LightConfigurationUploader? configUploader = new(mockHidDevice, true);

            LightConfiguration? config = configUploader.Download();

            Assert.IsTrue(config.EncodeModeData().AsSpan().SequenceEqual(modeData));
            Assert.IsTrue(config.EncodeStepData().AsSpan().SequenceEqual(stepData));
        }
    }
}
