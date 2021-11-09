using System;
using System.Linq;
using KnogUsbUtility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

[TestClass]
public class LightConfigurationUploaderTests {
    private readonly byte[] setupData = new byte[] { 0x00, 0x24, 0x04, 0x31, 0xa5, 0xf1, 0x01 };
    private readonly byte[] ackData = new byte[] { 0x00, 0x40 };

    [TestMethod]
    public void TestUpload() {
        MockHidDevice mockHidDevice = new();

        mockHidDevice.ExpectWrite(setupData);
        mockHidDevice.ReturnRead(ackData);

        mockHidDevice.ExpectWrite(new byte[] {
                0x00, 0x24, 0x2b, 0x32, 0xf8, 0x00, 0x00, 0x00,
                0x05, 0x06, 0x0a, 0x16, 0x16, 0x16, 0x16, 0x16,
                0x2f, 0x50, 0x64, 0xff, 0x0d, 0x64, 0x07, 0x3c,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0x01, 0x01, 0x01, 0x01, 0x07, 0x07, 0x07, 0x07,
                0x02, 0x07, 0x07, 0xff, 0xff, 0xff
            });
        mockHidDevice.ReturnRead(ackData);

        mockHidDevice.ExpectWrite(new byte[] {
                0x00, 0x24, 0x19, 0x32, 0xf8, 0x40, 0x3e, 0x3e,
                0x3e, 0x3e, 0xee, 0x3e, 0x1e, 0x34, 0x1e, 0x3a,
                0x28, 0x1e, 0x1e, 0x24, 0x1e, 0x1e, 0x22, 0x1e,
                0x1e, 0x24, 0x1e, 0x1e
            });
        mockHidDevice.ReturnRead(ackData);

        LightConfigurationUploader? configUploader = new(mockHidDevice, true);

        configUploader.Upload(
            LightConfiguration.Decode(LightConfigurationTests.ModeData, LightConfigurationTests.StepData));
    }

    [TestMethod]
    public void TestLargeUpload() {
        MockHidDevice mockHidDevice = new();

        LightConfiguration lightConfiguration = new() {
            Modes = new Mode[] {
                    new Mode {
                        ShortDelay = 1,
                        LongDelay = 255,
                        Steps = Enumerable.Range(0, 256)
                            .Select(i => Step.FromByte((byte)i))
                            .ToList(),
                    },
                },
        };

        mockHidDevice.ExpectWrite(setupData);
        mockHidDevice.ReturnRead(ackData);

        mockHidDevice.ExpectWrite(
            new byte[] { 0x00, 0x24, 0x2b, 0x32, 0xf8, 0x00, }
            .Concat(lightConfiguration.EncodeModeData()).ToArray());
        mockHidDevice.ReturnRead(ackData);

        byte[] stepData = lightConfiguration.EncodeStepData();

        for (int i = 0; i < 256; i += 64) {
            int addr = 0xf840 + i;

            mockHidDevice.ExpectWrite(
                new byte[] { 0x00, 0x24, 0x43, 0x32,
                            (byte)(addr >> 8), (byte)(addr & 0xFF), }
                .Concat(stepData[i..(i + 59)]).ToArray());

            mockHidDevice.ExpectWrite(
                new byte[] { 0x00 }
                .Concat(stepData[(i + 59)..(i + 64)]).ToArray());

            mockHidDevice.ReturnRead(ackData);
        }

        LightConfigurationUploader? configUploader = new(mockHidDevice, true);

        configUploader.Upload(lightConfiguration);
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

        Expect(LightConfigurationUploader.ProductCodeAddress, 8);

        byte[] modeData = LightConfigurationTests.ModeData;

        for (int i = 0; i < modeData.Length; i++) {
            Expect(
                LightConfigurationUploader.LightModesStartAddress + i,
                modeData[i]);
        }

        byte[] stepData = LightConfigurationTests.StepData;

        for (int i = 0; i < LightConfiguration.StepDataMaxSize; i++) {
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
