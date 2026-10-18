using System;
using System.Collections.Generic;
using System.IO;

namespace Unity.Collections { public enum Allocator { Temp } }
namespace Unity.Netcode
{
    public enum NetworkDelivery { ReliableSequenced }
    public sealed class FastBufferWriter : IDisposable
    {
        private readonly MemoryStream stream = new();
        private readonly BinaryWriter writer;
        public FastBufferWriter(int size, Unity.Collections.Allocator allocator) => writer = new(stream);
        public void WriteValueSafe(ulong value) => writer.Write(value);
        public void WriteValueSafe(float value) => writer.Write(value);
        public void WriteValueSafe(bool value) => writer.Write(value);
        public void WriteValueSafe(int value) => writer.Write(value);
        public byte[] Bytes => stream.ToArray();
        public void Dispose() => writer.Dispose();
    }
    public sealed class FastBufferReader
    {
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        public FastBufferReader(byte[] bytes) { stream = new(bytes); reader = new(stream); }
        public int Length => (int)stream.Length;
        public int Position => (int)stream.Position;
        public void ReadValueSafe(out ulong value) => value = reader.ReadUInt64();
        public void ReadValueSafe(out float value) => value = reader.ReadSingle();
        public void ReadValueSafe(out bool value) => value = reader.ReadBoolean();
        public void ReadValueSafe(out int value) => value = reader.ReadInt32();
    }
    public sealed record RefillPacket(ulong Sender, ulong Target, string Name, byte[] Bytes);
    public sealed class CustomMessagingManager
    {
        public static readonly Queue<RefillPacket> Pending = new();
        public readonly Dictionary<string, Action<ulong, FastBufferReader>> Handlers = new();
        public readonly List<RefillPacket> Attempts = new();
        private readonly NetworkManager owner;
        public int FailSends;
        public CustomMessagingManager(NetworkManager owner) => this.owner = owner;
        public void RegisterNamedMessageHandler(string name, Action<ulong, FastBufferReader> handler) => Handlers.Add(name, handler);
        public void UnregisterNamedMessageHandler(string name) => Handlers.Remove(name);
        public void SendNamedMessage(string name, ulong target, FastBufferWriter writer, NetworkDelivery delivery)
        {
            var packet = new RefillPacket(owner.LocalClientId, target, name, writer.Bytes);
            Attempts.Add(packet);
            if (FailSends > 0) { FailSends--; throw new IOException("Simulated send failure"); }
            Pending.Enqueue(packet);
        }
        public void Deliver(RefillPacket packet)
        {
            if (Handlers.TryGetValue(packet.Name, out var handler)) handler(packet.Sender, new(packet.Bytes));
        }
    }
}
