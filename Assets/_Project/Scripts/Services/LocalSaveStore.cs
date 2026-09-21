using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SkyOfFreedom.Services
{
    public sealed class LocalSaveRecord
    {
        public long Sequence;
        public string PlayerId;
        public bool Pending;
        public string BaseFingerprint;
        public string Json;
    }

    // Two independent, checksummed slots. Never truncate the newest valid slot.
    // This is recovery storage, not encryption or an anti-cheat mechanism.
    public sealed class LocalSaveStore
    {
        private const int Magic = 0x534F4631;
        private const int MaxBytes = 16 * 1024 * 1024;
        private readonly string directory;

        public LocalSaveStore(string directory)
        {
            this.directory = Path.GetFullPath(directory);
        }

        public static string Fingerprint(string json)
        {
            if (json == null) return string.Empty;
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "");
        }

        private string Slot(string playerId, int index)
        {
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Missing player ID.");
            return Path.Combine(directory, Fingerprint(playerId) + "." + index + ".save");
        }

        public LocalSaveRecord Read(string playerId)
        {
            LocalSaveRecord first = ReadSlot(Slot(playerId, 0), playerId);
            LocalSaveRecord second = ReadSlot(Slot(playerId, 1), playerId);
            LocalSaveRecord latest = first == null ? second : second == null ? first :
                first.Sequence >= second.Sequence ? first : second;
            if (latest == null && (File.Exists(Slot(playerId, 0)) || File.Exists(Slot(playerId, 1))))
                throw new InvalidDataException("Both local save slots are invalid. Recovery files were preserved.");
            return latest;
        }

        public LocalSaveRecord Write(string playerId, string json, string baseFingerprint, bool pending)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Missing save snapshot.");
            LocalSaveRecord first = ReadSlot(Slot(playerId, 0), playerId);
            LocalSaveRecord second = ReadSlot(Slot(playerId, 1), playerId);
            LocalSaveRecord latest = Read(playerId);
            var record = new LocalSaveRecord
            {
                Sequence = checked((latest?.Sequence ?? 0) + 1), PlayerId = playerId,
                Json = json, BaseFingerprint = baseFingerprint ?? string.Empty, Pending = pending
            };
            int target = first == null ? 0 : second == null ? 1 : first.Sequence <= second.Sequence ? 0 : 1;
            Directory.CreateDirectory(directory);
            byte[] bytes = Encode(record);
            using (var stream = new FileStream(Slot(playerId, target), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            return record;
        }

        public void Acknowledge(string playerId, string uploadedJson)
        {
            LocalSaveRecord latest = Read(playerId);
            if (latest == null) return;
            string uploaded = Fingerprint(uploadedJson);
            // A slow upload must not mark a newer local operation as synchronized.
            Write(playerId, latest.Json, uploaded, Fingerprint(latest.Json) != uploaded);
        }

        public static bool NeedsChoice(LocalSaveRecord local, string cloudJson)
        {
            if (local == null || !local.Pending) return false;
            string cloud = Fingerprint(cloudJson);
            return Fingerprint(local.Json) != cloud && local.BaseFingerprint != cloud;
        }

        public void Archive(LocalSaveRecord local)
        {
            if (local == null) return;
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, Fingerprint(local.PlayerId) + ".conflict-" +
                Guid.NewGuid().ToString("N") + ".save");
            byte[] bytes = Encode(local);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static byte[] Encode(LocalSaveRecord record)
        {
            byte[] payload;
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Magic); writer.Write(record.Sequence); writer.Write(record.PlayerId);
                    writer.Write(record.Pending); writer.Write(record.BaseFingerprint); writer.Write(record.Json);
                }
                payload = memory.ToArray();
            }
            if (payload.Length > MaxBytes) throw new InvalidDataException("Save snapshot is too large.");
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            using (SHA256 sha = SHA256.Create())
            {
                writer.Write(payload.Length); writer.Write(payload); writer.Write(sha.ComputeHash(payload));
                return memory.ToArray();
            }
        }

        private static LocalSaveRecord ReadSlot(string path, string playerId)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(stream))
                using (SHA256 sha = SHA256.Create())
                {
                    int length = reader.ReadInt32();
                    if (length <= 0 || length > MaxBytes || stream.Length != length + 36L) return null;
                    byte[] payload = reader.ReadBytes(length);
                    byte[] checksum = reader.ReadBytes(32);
                    byte[] expected = sha.ComputeHash(payload);
                    for (int i = 0; i < expected.Length; i++) if (checksum[i] != expected[i]) return null;
                    using (var memory = new MemoryStream(payload))
                    using (var data = new BinaryReader(memory, Encoding.UTF8))
                    {
                        if (data.ReadInt32() != Magic) return null;
                        var record = new LocalSaveRecord
                        {
                            Sequence = data.ReadInt64(), PlayerId = data.ReadString(), Pending = data.ReadBoolean(),
                            BaseFingerprint = data.ReadString(), Json = data.ReadString()
                        };
                        return record.Sequence > 0 && record.PlayerId == playerId &&
                            !string.IsNullOrWhiteSpace(record.Json) && memory.Position == memory.Length ? record : null;
                    }
                }
            }
            catch (EndOfStreamException) { return null; }
            catch (FormatException) { return null; }
            // Permission/disk failures propagate: they are not evidence of an empty save.
        }
    }
}
