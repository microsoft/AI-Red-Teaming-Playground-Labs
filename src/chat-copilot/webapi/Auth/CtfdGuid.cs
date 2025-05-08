// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;

namespace CopilotChat.WebApi.Auth;

public class CtfdGuid
{
    public Guid GuidValue { private set; get; }
    private static readonly byte[] CtfdDefaultBytes = new byte[] { 0xc8, 0x16, 0xd3, 0x69, 0x1e, 0x9d, 0x34, 0x86, 0xb7, 0x6e, 0xff, 0xff };

    public CtfdGuid(int id)
    {
        this.SetGuid(id);
    }

    public CtfdGuid(string guidValue)
    {
        this.GuidValue = Guid.Parse(guidValue);
    }

    public CtfdGuid(Guid guidValue)
    {
        this.GuidValue = guidValue;
    }

    private void SetGuid(int id)
    {
        var userIdBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(id));

        // Combine the two arrays
        var guidBytes = new byte[CtfdDefaultBytes.Length + userIdBytes.Length];
        Buffer.BlockCopy(CtfdDefaultBytes, 0, guidBytes, 0, CtfdDefaultBytes.Length);
        Buffer.BlockCopy(userIdBytes, 0, guidBytes, CtfdDefaultBytes.Length, userIdBytes.Length);

        this.GuidValue = new Guid(guidBytes);
    }

    public int GetUserId()
    {
        var guidBytes = this.GuidValue.ToByteArray();
        var userIdBytes = new byte[4];
        Buffer.BlockCopy(guidBytes, CtfdDefaultBytes.Length, userIdBytes, 0, userIdBytes.Length);

        var userId = BinaryPrimitives.ReadInt32BigEndian(userIdBytes);
        return userId;
    }

    public override string ToString()
    {
        return this.GuidValue.ToString();
    }
}
