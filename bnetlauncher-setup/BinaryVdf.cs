// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace bnetlauncher_setup
{
    /// <summary>
    /// Minimal reader/writer for Valve's "binary VDF" format, used by Steam's
    /// userdata\&lt;id&gt;\config\shortcuts.vdf. It's an undocumented but widely reverse
    /// engineered format (the same one used by Steam ROM Manager, Boilr, etc).
    ///
    /// Layout: a sequence of typed, named entries. A file/object is just a sequence of
    /// entries terminated by 0x08 (or end-of-file for the implicit top level object).
    ///   0x00 = nested object      : type, null-terminated key, child entries..., 0x08
    ///   0x01 = string value       : type, null-terminated key, null-terminated value
    ///   0x02 = int32 value        : type, null-terminated key, 4 bytes little-endian
    ///
    /// Parsing generically (instead of hardcoding known fields) means any existing
    /// shortcuts entries and fields we don't know about round-trip untouched.
    /// </summary>
    internal sealed class VdfNode
    {
        public const byte TypeObject = 0x00;
        public const byte TypeString = 0x01;
        public const byte TypeInt32 = 0x02;
        private const byte TypeEnd = 0x08;

        // Ordered so re-serializing keeps the same entry order Steam wrote.
        public List<(string Key, byte Type, object Value)> Children { get; } = new List<(string, byte, object)>();

        public VdfNode GetOrAddObject(string key)
        {
            foreach (var c in Children)
            {
                if (c.Type == TypeObject && string.Equals(c.Key, key, StringComparison.Ordinal))
                {
                    return (VdfNode)c.Value;
                }
            }
            var node = new VdfNode();
            Children.Add((key, TypeObject, node));
            return node;
        }

        public void SetString(string key, string value)
        {
            RemoveKey(key);
            Children.Add((key, TypeString, value ?? string.Empty));
        }

        public void SetInt32(string key, int value)
        {
            RemoveKey(key);
            Children.Add((key, TypeInt32, value));
        }

        public void RemoveKey(string key)
        {
            Children.RemoveAll(c => string.Equals(c.Key, key, StringComparison.Ordinal));
        }

        public string GetString(string key)
        {
            foreach (var c in Children)
            {
                if (c.Type == TypeString && string.Equals(c.Key, key, StringComparison.Ordinal))
                {
                    return (string)c.Value;
                }
            }
            return null;
        }

        public static VdfNode Load(string path)
        {
            if (!File.Exists(path))
            {
                return new VdfNode();
            }

            var data = File.ReadAllBytes(path);
            var pos = 0;
            return ParseSequence(data, ref pos);
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            // Written to a temp file first so a failure mid-write can't leave the user's
            // shortcuts.vdf truncated.
            var tempPath = path + ".tmp";
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                WriteChildren(writer, this);
                // Steam terminates the implicit top-level object too, with one more 0x08
                // after "shortcuts" own closing byte. Verified by byte-comparing against a
                // real shortcuts.vdf written by Steam itself.
                writer.Write(TypeEnd);
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        private static VdfNode ParseSequence(byte[] data, ref int pos)
        {
            var node = new VdfNode();
            while (pos < data.Length)
            {
                var type = data[pos];
                if (type == TypeEnd)
                {
                    pos++;
                    return node;
                }

                pos++;
                var key = ReadCString(data, ref pos);

                switch (type)
                {
                    case TypeObject:
                        node.Children.Add((key, type, ParseSequence(data, ref pos)));
                        break;
                    case TypeString:
                        node.Children.Add((key, type, ReadCString(data, ref pos)));
                        break;
                    case TypeInt32:
                        node.Children.Add((key, type, BitConverter.ToInt32(data, pos)));
                        pos += 4;
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Unknown binary VDF field type 0x{type:X2} at offset {pos - 1}. " +
                            "The file may be corrupt or in a newer format than expected.");
                }
            }

            return node; // reached EOF: valid for the implicit top-level object
        }

        private static void WriteChildren(BinaryWriter writer, VdfNode node)
        {
            foreach (var (key, type, value) in node.Children)
            {
                writer.Write(type);
                WriteCString(writer, key);

                switch (type)
                {
                    case TypeObject:
                        WriteChildren(writer, (VdfNode)value);
                        writer.Write(TypeEnd);
                        break;
                    case TypeString:
                        WriteCString(writer, (string)value);
                        break;
                    case TypeInt32:
                        writer.Write((int)value);
                        break;
                }
            }
        }

        private static string ReadCString(byte[] data, ref int pos)
        {
            var start = pos;
            while (pos < data.Length && data[pos] != 0)
            {
                pos++;
            }
            var s = Encoding.UTF8.GetString(data, start, pos - start);
            pos++; // skip the null terminator
            return s;
        }

        private static void WriteCString(BinaryWriter writer, string s)
        {
            writer.Write(Encoding.UTF8.GetBytes(s ?? string.Empty));
            writer.Write((byte)0);
        }
    }
}
