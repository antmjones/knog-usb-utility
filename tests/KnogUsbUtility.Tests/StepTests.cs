using KnogUsbUtility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass]
    public class StepTests {
        [TestMethod]
        public void TestStepParse() {
            Step step = Step.FromPattern("3 L -O-");

            Assert.AreEqual(3, step.Brightness);
            Assert.IsTrue(step.UseLongDelay);
            Assert.IsFalse(step.Channel1);
            Assert.IsTrue(step.Channel2);
            Assert.IsFalse(step.Channel3);
        }

        [TestMethod]
        [DataRow("3 L -O-")]
        [DataRow("0 S OOO")]
        [DataRow("7 S ---")]
        public void TestStepToString(string pattern) =>
            Assert.AreEqual(pattern, Step.FromPattern(pattern).ToString());

        [TestMethod]
        [DataRow("3 L -O-", (byte)116)]
        [DataRow("0 S OOO", (byte)14)]
        [DataRow("7 S ---", (byte)224)]
        public void TestStepConvertToByte(string pattern, byte expected) =>
            Assert.AreEqual(expected, Step.FromPattern(pattern).ConvertToByte());

        [TestMethod]
        [DataRow("3 L -O-", (byte)116)]
        [DataRow("0 S OOO", (byte)14)]
        [DataRow("7 S ---", (byte)224)]
        public void TestStepFromByte(string pattern, byte expected) =>
            Assert.AreEqual(pattern, Step.FromByte(expected).ToString());
    }
}
