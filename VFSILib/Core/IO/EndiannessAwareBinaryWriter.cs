using System;
using System.IO;
using System.Text;
using VFSILib.Common.Enum;

namespace VFSILib.Core.IO;

public class EndiannessAwareBinaryWriter : BinaryWriter
{
    public EndiannessAwareBinaryWriter(Stream input) : base(input)
    {
    }

    public EndiannessAwareBinaryWriter(Stream input, Encoding encoding) : base(input, encoding)
    {
    }

    public EndiannessAwareBinaryWriter(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding,
        leaveOpen)
    {
    }

    public EndiannessAwareBinaryWriter(Stream input, ByteOrder endianness) : base(input)
    {
        Endianness = endianness;
    }

    public EndiannessAwareBinaryWriter(Stream input, Encoding encoding, ByteOrder endianness) : base(
        input, encoding)
    {
        Endianness = endianness;
    }

    public EndiannessAwareBinaryWriter(Stream input, Encoding encoding, bool leaveOpen,
        ByteOrder endianness) : base(input, encoding, leaveOpen)
    {
        Endianness = endianness;
    }

    public ByteOrder Endianness { get; private set; } = ByteOrder.LittleEndian;

    public override void Write(byte[] buffer)
    {
        Write(buffer, Endianness);
    }

    public override void Write(short value)
    {
        Write(value, Endianness);
    }

    public override void Write(int value)
    {
        Write(value, Endianness);
    }

    public override void Write(long value)
    {
        Write(value, Endianness);
    }

    public override void Write(ushort value)
    {
        Write(value, Endianness);
    }

    public override void Write(uint value)
    {
        Write(value, Endianness);
    }

    public override void Write(ulong value)
    {
        Write(value, Endianness);
    }

    public override void Write(float value)
    {
        Write(value, Endianness);
    }

    public override void Write(double value)
    {
        Write(value, Endianness);
    }

    public void Write(byte[] buffer, ByteOrder endianness)
    {
        WriteWithEndianness(buffer, endianness);
    }

    public void Write(short value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(int value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(long value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(ushort value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(uint value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(ulong value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(float value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void Write(double value, ByteOrder endianness)
    {
        WriteWithEndianness(BitConverter.GetBytes(value), endianness);
    }

    public void ChangeEndianness(ByteOrder endianness)
    {
        Endianness = endianness;
    }

    private void WriteWithEndianness(byte[] buffer, ByteOrder endianness)
    {
        switch (endianness)
        {
            case ByteOrder.LittleEndian:
                if (!BitConverter.IsLittleEndian) Array.Reverse(buffer);
                break;

            case ByteOrder.BigEndian:
                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
                break;
        }

        base.Write(buffer);
    }
}