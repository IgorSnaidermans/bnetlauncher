// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Text;

namespace bnetlauncher_setup
{
    /// <summary>
    /// Standard CRC-32 (polynomial 0xEDB88320, same as zlib/PKZIP), used by Steam to derive
    /// a non-Steam game's "appid" from its Exe path and display name.
    /// </summary>
    internal static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                var c = i;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                }
                table[i] = c;
            }
            return table;
        }

        public static uint Compute(byte[] bytes)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in bytes)
            {
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFF;
        }

        /// <summary>
        /// Steam's "legacy" non-Steam game id: CRC32 of (Exe + AppName) with the top bit set.
        /// Steam recomputes/accepts this value for shortcut grid art and Big Picture lookups.
        /// </summary>
        public static uint GenerateShortcutAppId(string exe, string appName)
        {
            var key = exe + appName;
            var crc = Compute(Encoding.UTF8.GetBytes(key));
            return crc | 0x80000000;
        }
    }
}
