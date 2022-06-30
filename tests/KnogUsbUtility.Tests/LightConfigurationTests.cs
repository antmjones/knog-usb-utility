using System;
using System.Text.Json;
using KnogUsbUtility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

[TestClass]
public class LightConfigurationTests {
    public static byte[] ModeData => new byte[] {
        0x00, 0x00, 0x05, 0x06, 0x0A, 0x16, 0x16, 0x16, 0x16, 0x16, // step data
        0x2F, 0x50, 0x64, 0xFF, 0x0D, 0x64, 0x07, 0x3C, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // delay periods
        0x01, 0x01, 0x01, 0x01, 0x07, 0x07, 0x07, 0x07, // mode status
        0x02, 0x07, 0x07, 0xFF, 0xFF, 0xFF, // button data
    };

    public static byte[] StepData => new byte[] {
        0x3E, 0x3E, 0x3E, 0x3E, 0xEE, 0x3E, 0x1E, 0x34, 0x1E, 0x3A, 0x28, 0x1E, 0x1E, 0x24, 0x1E, 0x1E, 0x22, 0x1E, 0x1E, 0x24, 0x1E, 0x1E,
    };

    [TestMethod]
    public void TestDecodeAndEncode() {
        var config = LightConfiguration.Decode(ModeData, StepData);

        Assert.AreEqual(4, config.Modes.Count);

        Assert.AreEqual(config.Modes[0].ShortDelay, 0x2F);
        Assert.AreEqual(config.Modes[0].LongDelay, 0x50);
        Assert.AreEqual(config.Modes[1].ShortDelay, 0x64);
        Assert.AreEqual(config.Modes[1].LongDelay, 0xFF);
        Assert.AreEqual(config.Modes[2].ShortDelay, 0x0D);
        Assert.AreEqual(config.Modes[2].LongDelay, 0x64);
        Assert.AreEqual(config.Modes[3].ShortDelay, 0x07);
        Assert.AreEqual(config.Modes[3].LongDelay, 0x3C);

        Assert.IsTrue(config.Modes[0].IsEnabled);
        Assert.IsTrue(config.Modes[1].IsEnabled);
        Assert.IsTrue(config.Modes[2].IsEnabled);
        Assert.IsTrue(config.Modes[3].IsEnabled);

        Assert.AreEqual("1 L OOO", config.Modes[0].Steps[0].ToString());
        Assert.AreEqual("1 L OOO", config.Modes[0].Steps[1].ToString());
        Assert.AreEqual("1 L OOO", config.Modes[0].Steps[2].ToString());
        Assert.AreEqual("1 L OOO", config.Modes[0].Steps[3].ToString());
        Assert.AreEqual("7 S OOO", config.Modes[0].Steps[4].ToString());

        Assert.AreEqual("1 L OOO", config.Modes[1].Steps[0].ToString());

        Assert.AreEqual("0 L OOO", config.Modes[2].Steps[0].ToString());
        Assert.AreEqual("1 L -O-", config.Modes[2].Steps[1].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[2].Steps[2].ToString());
        Assert.AreEqual("1 L O-O", config.Modes[2].Steps[3].ToString());

        Assert.AreEqual("1 S O--", config.Modes[3].Steps[0].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[1].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[2].ToString());
        Assert.AreEqual("1 S -O-", config.Modes[3].Steps[3].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[4].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[5].ToString());
        Assert.AreEqual("1 S --O", config.Modes[3].Steps[6].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[7].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[8].ToString());
        Assert.AreEqual("1 S -O-", config.Modes[3].Steps[9].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[10].ToString());
        Assert.AreEqual("0 L OOO", config.Modes[3].Steps[11].ToString());

        byte[] modeDataReencoded = config.EncodeModeData();
        byte[] stepDataReencoded = config.EncodeStepData();

        Assert.IsTrue(modeDataReencoded.AsSpan().SequenceEqual(ModeData));
        Assert.IsTrue(stepDataReencoded.AsSpan().SequenceEqual(StepData));
    }

    [TestMethod]
    public void TestJsonRoundTrip() {
        string json = JsonSerializer.Serialize(
            LightConfiguration.Decode(ModeData, StepData));

        LightConfiguration? deserializedConfig =
            JsonSerializer.Deserialize<LightConfiguration>(json);

        Assert.IsNotNull(deserializedConfig);

        byte[] modeDataReencoded = deserializedConfig.EncodeModeData();
        byte[] stepDataReencoded = deserializedConfig.EncodeStepData();

        Assert.IsTrue(modeDataReencoded.AsSpan().SequenceEqual(ModeData));
        Assert.IsTrue(stepDataReencoded.AsSpan().SequenceEqual(StepData));
    }
}
