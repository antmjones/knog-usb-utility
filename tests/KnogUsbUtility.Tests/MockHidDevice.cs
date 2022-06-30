using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HidLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests;

// this is purely a mock class, we don't need to worry about having a "correct" API
#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
#pragma warning disable CA2201 // Do not raise reserved exception types

public class MockHidDevice : IHidDevice {
    private readonly Queue<byte[]> expectedWrites = new();
    private readonly Queue<byte[]> returnedReads = new();

    public MockHidDevice() {
    }

    public void ExpectWrite(byte[] data) =>
        expectedWrites.Enqueue(data);

    public void ReturnRead(byte[] data) =>
        returnedReads.Enqueue(data);

    public IntPtr ReadHandle => throw new NotImplementedException();

    public IntPtr WriteHandle => throw new NotImplementedException();

    public bool IsOpen => throw new NotImplementedException();

    public bool IsConnected => throw new NotImplementedException();

    public string Description => throw new NotImplementedException();

    public HidDeviceCapabilities Capabilities => throw new NotImplementedException();

    public HidDeviceAttributes Attributes => throw new NotImplementedException();

    public string DevicePath => throw new NotImplementedException();

    public bool MonitorDeviceEvents { 
        get => throw new NotImplementedException(); 
        set => throw new NotImplementedException(); 
    }

    public event InsertedEventHandler Inserted {
        add => throw new NotSupportedException();
        remove => throw new NotSupportedException();
    }

    public event RemovedEventHandler Removed {
        add => throw new NotSupportedException();
        remove => throw new NotSupportedException();
    }

    public void CloseDevice() {
    }

    public HidReport CreateReport() =>
        throw new NotImplementedException();

    public void Dispose() {
        if (returnedReads.Count > 0) {
            throw new Exception("Not all reads returned");
        }

        if (expectedWrites.Count > 0) {
            throw new Exception("Not all writes received");
        }

        GC.SuppressFinalize(this);
    }

    public void OpenDevice() =>
        throw new NotImplementedException();

    public void OpenDevice(DeviceMode readMode, DeviceMode writeMode, ShareMode shareMode) =>
        throw new NotImplementedException();

    public HidDeviceData Read() =>
        new(returnedReads.Dequeue(), HidDeviceData.ReadStatus.Success);

    public void Read(ReadCallback callback) =>
        throw new NotImplementedException();

    public void Read(ReadCallback callback, int timeout) =>
        throw new NotImplementedException();

    public HidDeviceData Read(int timeout) =>
        throw new NotImplementedException();

    public Task<HidDeviceData> ReadAsync(int timeout = 0) =>
        throw new NotImplementedException();

    public bool ReadFeatureData(out byte[] data, byte reportId = 0) =>
        throw new NotImplementedException();

    public bool ReadManufacturer(out byte[] data) =>
        throw new NotImplementedException();

    public bool ReadProduct(out byte[] data) =>
        throw new NotImplementedException();

    public void ReadReport(ReadReportCallback callback) =>
        throw new NotImplementedException();

    public void ReadReport(ReadReportCallback callback, int timeout) =>
        throw new NotImplementedException();

    public HidReport ReadReport(int timeout) =>
        throw new NotImplementedException();

    public HidReport ReadReport() =>
        throw new NotImplementedException();

    public Task<HidReport> ReadReportAsync(int timeout = 0) =>
        throw new NotImplementedException();

    public bool ReadSerialNumber(out byte[] data) =>
        throw new NotImplementedException();

    public void Write(byte[] data, WriteCallback callback) =>
        throw new NotImplementedException();

    public bool Write(byte[] data) {
        byte[] expected = expectedWrites.Dequeue();
        Assert.IsTrue(data.AsSpan().SequenceEqual(expected));
        return true;
    }

    public bool Write(byte[] data, int timeout) =>
        throw new NotImplementedException();

    public void Write(byte[] data, WriteCallback callback, int timeout) =>
        throw new NotImplementedException();

    public Task<bool> WriteAsync(byte[] data, int timeout = 0) =>
        throw new NotImplementedException();

    public bool WriteFeatureData(byte[] data) =>
        throw new NotImplementedException();

    public void WriteReport(HidReport report, WriteCallback callback) =>
        throw new NotImplementedException();

    public bool WriteReport(HidReport report) =>
        throw new NotImplementedException();

    public bool WriteReport(HidReport report, int timeout) =>
        throw new NotImplementedException();

    public void WriteReport(HidReport report, WriteCallback callback, int timeout) =>
        throw new NotImplementedException();

    public Task<bool> WriteReportAsync(HidReport report, int timeout = 0) =>
        throw new NotImplementedException();
}
