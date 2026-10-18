using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oxygen.Configuration
{
    // Versioned, keyed settings. Never apply a partially received snapshot.
    internal sealed class ConfigSnapshot
    {
        internal const int ProtocolVersion = 1;
        internal const int MaxEntries = 256;
        internal const int ChunkSize = 8192;
        internal const int MaxBytes = 4 * 1024 * 1024;
        private byte[] pending;
        private int received;

        internal static byte[] Encode(IDictionary<string, string> values)
        {
            if (values.Count == 0 || values.Count > MaxEntries) throw new InvalidDataException("Invalid host config count.");
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(ProtocolVersion);
            writer.Write(values.Count);
            foreach (var entry in values)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value ?? string.Empty);
                if (stream.Length > MaxBytes) throw new InvalidDataException("Host config exceeds 4 MiB.");
            }
            return stream.ToArray();
        }

        internal Dictionary<string, string> Receive(int totalBytes, int offset, byte[] chunk)
        {
            if (totalBytes <= 0 || totalBytes > MaxBytes || offset < 0 ||
                chunk.Length == 0 || chunk.Length > ChunkSize || offset > totalBytes - chunk.Length)
                throw new InvalidDataException("Invalid host config chunk size.");

            if (offset == 0)
            {
                pending = new byte[totalBytes];
                received = 0;
            }
            if (pending == null || pending.Length != totalBytes || offset != received)
                throw new InvalidDataException("Host config chunks arrived out of order.");

            Buffer.BlockCopy(chunk, 0, pending, offset, chunk.Length);
            received += chunk.Length;
            if (received != totalBytes) return null;

            using var stream = new MemoryStream(pending, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadInt32() != ProtocolVersion) throw new InvalidDataException("Incompatible Oxygen config protocol.");
            int count = reader.ReadInt32();
            if (count <= 0 || count > MaxEntries) throw new InvalidDataException("Invalid host config count.");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                if (values.ContainsKey(key)) throw new InvalidDataException("Duplicate host config key.");
                values.Add(key, reader.ReadString());
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Unexpected host config data.");
            Reset();
            return values;
        }

        internal void Reset()
        {
            pending = null;
            received = 0;
        }
    }
}
