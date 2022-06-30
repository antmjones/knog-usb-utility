using System;
using System.Linq;
using HidLibrary;

namespace KnogUsbUtility;

public class LightConfigurationUploader : IDisposable {
    private const int VendorId = 0x10c4;
    private const int ProductId = 0xeac9; // EFM8UB1

    public const int LightModesStartAddress = 0xF800;
    public const int StepDataStartAddress = 0xF840;
    public const int ProductCodeAddress = 0xFB40;
    public const int EndAddressForDump = 0xFB7F;

    private const byte FrameStartByte = 0x24;
    private const byte ReportNumber = 0;

    private const int PageSize = 64;

    private enum Command : byte {
        Setup = 0x31,
        Erase = 0x32,
        RNVM = 0x38, // can't find any reference to this in silabs documentation...
    }

    private const byte CobberMidRearProductCode = 0x08;

    private readonly IHidDevice hidDevice;
    private readonly bool autoCloseHidDevice;

    public LightConfigurationUploader(IHidDevice hidDevice, bool autoCloseHidDevice) {
        this.hidDevice = hidDevice;
        this.autoCloseHidDevice = autoCloseHidDevice;
    }

    private static IHidDevice FindDevice() {
        IHidDevice? device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();

        if (device == null) {
            throw new DeviceCommunicationException("Cannot find device");
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

    public byte ReadByte(int addr) {
        WriteFrame(
            Command.RNVM,
            new byte[] {
                (byte)((addr >> 8) & 0xFF),
                (byte)(addr & 0xFF),
                0x01
            });

        HidDeviceData? readResult = hidDevice.Read();

        if (readResult.Status != HidDeviceData.ReadStatus.Success) {
            throw new DeviceCommunicationException("Read failed, status was " + readResult.Status);
        }

        return readResult.Data[1];
    }

    private void WriteFrame(Command command, byte[] data) {
        // https://www.silabs.com/documents/public/application-notes/an945-efm8-factory-bootloader-user-guide.pdf
        // Section 7 "Bootloader Protocol"
        byte[] header = {
            FrameStartByte,
            (byte)(data.Length + 1),
            (byte)command,
        };

        byte[] frame = Append(header, data);

        // If I understand correctly this should use
        // hidDevice.Capabilities.OutputReportByteLength
        // but that makes mocking for unit testing unnecessarily difficult
        // since HidDeviceAttributes has no public constructor.
        const int maxFrameLength = 64;

        foreach (byte[] chunk in frame.Chunk(maxFrameLength)) {
            // first byte is the report # and needs to be included with every write
            bool result = hidDevice.Write(chunk.Prepend(ReportNumber).ToArray());

            // hidlibrary swallows all exceptions and just returns a boolean :(
            if (!result) {
                throw new DeviceCommunicationException("Write failed?");
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
            throw new DeviceCommunicationException("Read failed, status was " + response.Status);
        }

        if (response.Data.Length < 2 || response.Data[1] != 0x40) {
            // see https://www.silabs.com/documents/public/application-notes/an945-efm8-factory-bootloader-user-guide.pdf
            // section 7.2.
            throw new DeviceCommunicationException(
                "Did not receive an ACK for previous command: " + ToHexString(response.Data));
        }
    }

    public LightConfiguration Download() {
        byte productCode = ReadByte(ProductCodeAddress);

        if (productCode != CobberMidRearProductCode) {
            throw new NotImplementedException("Support for lights other than Cobber Mid Rear not tested.");
        }

        byte[] modeData = Read(LightModesStartAddress, LightConfiguration.ModeDataSize);
        byte[] stepData = Read(StepDataStartAddress, LightConfiguration.StepDataMaxSize);

        return LightConfiguration.Decode(modeData, stepData);
    }

    public void Upload(LightConfiguration configuration) {
        ArgumentNullException.ThrowIfNull(configuration);

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
