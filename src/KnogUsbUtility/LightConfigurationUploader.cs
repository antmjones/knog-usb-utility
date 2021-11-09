using System;
using System.IO;
using System.Linq;
using HidLibrary;

namespace KnogUsbUtility;

public class LightConfigurationUploader : IDisposable {
    private const int VendorId = 0x10c4;
    private const int ProductId = 0xeac9; // EFM8UB1

    public const int LightModesStartAddress = 0xF800;
    public const int StepDataStartAddress = 0xF840;
    public const int ProductCodeAddress = 0xFB40;

    private const int PageSize = 64;

    private enum Command : byte {
        Setup = 0x31,
        Erase = 0x32,
        RNVM = 0x38, // can't find any reference to this in silabs documentation...
    }

    private readonly IHidDevice hidDevice;
    private readonly bool autoCloseHidDevice;

    public LightConfigurationUploader(IHidDevice hidDevice, bool autoCloseHidDevice) {
        this.hidDevice = hidDevice;
        this.autoCloseHidDevice = autoCloseHidDevice;
    }

    private static IHidDevice FindDevice() {
        IHidDevice? device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();

        if (device == null) {
            throw new Exception("Cannot find device");
        }

        return device;
    }

    private byte[] Read(int start, int count) {
        byte[] bytes = new byte[count];

        for (int i = 0; i < bytes.Length; i++) {
            bytes[i] = ReadByte(start + i);
        }

        return bytes;
    }

    private static string ToHexString(byte[] data) =>
        "{ " + string.Join(", ", data.Select(d => $"0x{d:x2}")) + " }";

    public void DumpMemory(TextWriter writer) {
        for (int i = 0xF800; i <= 0xFB7F; i++) {
            byte b = ReadByte(i);
            writer.WriteLine($"0x{i:X4}: 0x{b:X2} ({b})");
        }
    }

    private byte ReadByte(int addr) {
        WriteFrame(
            Command.RNVM,
            new byte[] {
                    (byte)((addr >> 8) & 0xFF),
                    (byte)(addr & 0xFF),
                    0x01
            });

        HidDeviceData? readResult = hidDevice.Read();

        if (readResult.Status != HidDeviceData.ReadStatus.Success) {
            throw new Exception("Read failed, status was " + readResult.Status);
        }

        return readResult.Data[1];
    }

    private void WriteFrame(Command command, byte[] data) {
        byte[] header = {
            0x24,
            (byte)(data.Length + 1),
            (byte)command,
        };

        byte[] packet = Append(header, data);

        const int maxFrameLength = 64;

        foreach (byte[] frame in packet.Chunk(maxFrameLength)) {
            bool result = hidDevice.Write(frame.Prepend((byte)0).ToArray());

            if (!result) {
                throw new Exception("Write failed?");
            }
        }
    }

    private void StartSetup() {
        // https://www.silabs.com/documents/public/application-notes/an945-efm8-factory-bootloader-user-guide.pdf
        // Section 7.1 / Setup 0x31
        byte[] data = { 0xA5, 0xF1, 0x01 };
        WriteFrame(Command.Setup, data);
        CheckStatus();
    }

    private void Erase(int address, byte[] data) {
        if (address % PageSize != 0) {
            throw new ArgumentException("address must be page aligned");
        }

        foreach (byte[] page in data.Chunk(PageSize)) {
            byte[] header = {
                (byte)((address >> 8) & 0xFF),
                (byte)((address >> 0) & 0xFF),
            };

            WriteFrame(Command.Erase, Append(header, page));
            CheckStatus();

            address += PageSize;
        }

        //for (int i = 0; i < data.Length; i++) {
        //    if (ReadByte(address + i) != data[i]) {
        //        throw new Exception("Data not written correctly");
        //    }
        //}
    }

    private static byte[] Append(byte[] start, byte[] end) {
        byte[] result = new byte[start.Length + end.Length];

        start.CopyTo(result, 0);
        end.CopyTo(result, start.Length);

        return result;
    }

    private void CheckStatus() {
        HidDeviceData response = hidDevice.Read();

        if (response.Status != HidDeviceData.ReadStatus.Success) {
            throw new Exception("Read failed, status was " + response.Status);
        }

        if (response.Data.Length < 2 || response.Data[1] != 0x40) {
            // see https://www.silabs.com/documents/public/application-notes/an945-efm8-factory-bootloader-user-guide.pdf
            // section 7.2.
            throw new Exception("Did not receive an ACK for previous command: " + ToHexString(response.Data));
        }
    }

    public LightConfiguration Download() {
        byte productCode = ReadByte(ProductCodeAddress);

        if (productCode != 8) {
            throw new NotImplementedException("Support for lights other than Cobber Mid Rear not tested.");
        }

        byte[] modeData = Read(LightModesStartAddress, LightConfiguration.ModeDataSize);
        byte[] stepData = Read(StepDataStartAddress, LightConfiguration.StepDataMaxSize);

        return LightConfiguration.Decode(modeData, stepData);
    }

    public void Upload(LightConfiguration configuration) {
        byte[] stepData = configuration.EncodeStepData();
        byte[] modeData = configuration.EncodeModeData();

        StartSetup();
        Erase(LightModesStartAddress, modeData);
        Erase(StepDataStartAddress, stepData);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            if (autoCloseHidDevice) {
                hidDevice.Dispose();
            }
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static LightConfigurationUploader Create() {
        using IHidDevice hidDevice = FindDevice();
        return new LightConfigurationUploader(hidDevice, autoCloseHidDevice: true);
    }
}
