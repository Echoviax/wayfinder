using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

// This code is all taken from Yuna0x0's decompiler
namespace Wayfinder.Decompiler
{
    public enum FileType : byte
    {
        Unknown = 0,
        Assembly = 1,
        NativeBinary = 2,
        DepsJson = 3,
        RuntimeConfigJson = 4,
        Symbols = 5
    }

    public class BundleEntry
    {
        public long Offset { get; set; }
        public long Size { get; set; }
        public long CompressedSize { get; set; }
        public FileType FileType { get; set; }
        public string RelativePath { get; set; } = string.Empty;
    }

    public class BundleManifest
    {
        public uint MajorVersion { get; set; }
        public uint MinorVersion { get; set; }
        public List<BundleEntry> Entries { get; set; } = new();
        public string BundleId { get; set; } = string.Empty;
    }

    public static class Extractor
    {
        private static readonly byte[] BundleSignature = new byte[] {
            0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
            0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
            0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
            0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
        };

        public static BundleManifest? ParseBundle(byte[] data)
        {
            int sigPos = data.AsSpan().IndexOf(BundleSignature);
            if (sigPos == -1 || sigPos < 8) return null;

            long manifestOffset = BitConverter.ToInt64(data, sigPos - 8);
            if (manifestOffset == 0 || manifestOffset >= data.Length) return null;

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            ms.Position = manifestOffset;
            uint major = reader.ReadUInt32();
            uint minor = reader.ReadUInt32();
            int fileCount = reader.ReadInt32();

            string bundleId = reader.ReadString();

            if (major >= 2)
            {
                ms.Position += (8 * 4) + 8;
            }

            var entries = new List<BundleEntry>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                long offset = reader.ReadInt64();
                long size = reader.ReadInt64();
                long compressedSize = major >= 6 ? reader.ReadInt64() : 0;
                FileType fileType = (FileType)reader.ReadByte();
                string relativePath = reader.ReadString();

                entries.Add(new BundleEntry
                {
                    Offset = offset,
                    Size = size,
                    CompressedSize = compressedSize,
                    FileType = fileType,
                    RelativePath = relativePath
                });
            }

            return new BundleManifest
            {
                MajorVersion = major,
                MinorVersion = minor,
                BundleId = bundleId,
                Entries = entries
            };
        }

        public static List<string> ExtractBundle(string path, string outputDir)
        {
            byte[] data = File.ReadAllBytes(path);
            var manifest = ParseBundle(data);

            if (manifest == null)
                throw new InvalidOperationException($"No .NET single-file bundle found in {path}");

            Directory.CreateDirectory(outputDir);
            var extractedFiles = new List<string>();

            foreach (var entry in manifest.Entries)
            {
                string safePath = entry.RelativePath.TrimStart('/', '\\');
                string outPath = Path.GetFullPath(Path.Combine(outputDir, safePath));

                string? parentDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(parentDir))
                    Directory.CreateDirectory(parentDir);

                if (entry.CompressedSize > 0)
                {
                    using var ms = new MemoryStream(data, (int)entry.Offset, (int)entry.CompressedSize);
                    using var decompressor = new DeflateStream(ms, CompressionMode.Decompress);
                    using var outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write);
                    decompressor.CopyTo(outFile);
                }
                else
                {
                    using var outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write);
                    outFile.Write(data, (int)entry.Offset, (int)entry.Size);
                }

                extractedFiles.Add(outPath);
            }

            return extractedFiles;
        }
    }
}
