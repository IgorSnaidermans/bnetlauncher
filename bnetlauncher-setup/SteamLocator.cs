// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace bnetlauncher_setup
{
    internal sealed class SteamAccount
    {
        public string SteamId3 { get; set; }
        public string DisplayName { get; set; }
        public string ShortcutsVdfPath { get; set; }

        public override string ToString() =>
            string.IsNullOrEmpty(DisplayName) ? SteamId3 : $"{DisplayName} ({SteamId3})";
    }

    internal static class SteamLocator
    {
        public static string FindSteamInstallPath()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    var path = key?.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        return path.Replace('/', '\\');
                    }
                }
            }
            catch
            {
                // fall through to the other lookup / default below
            }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam") ??
                                  Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    var path = key?.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch
            {
                // ignore, try the common default below
            }

            var common = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            return Directory.Exists(common) ? common : null;
        }

        public static List<SteamAccount> FindAccounts(string steamPath)
        {
            var accounts = new List<SteamAccount>();
            if (string.IsNullOrEmpty(steamPath))
            {
                return accounts;
            }

            var userdata = Path.Combine(steamPath, "userdata");
            if (!Directory.Exists(userdata))
            {
                return accounts;
            }

            var personaNames = TryReadPersonaNames(Path.Combine(steamPath, "config", "loginusers.vdf"));

            foreach (var dir in Directory.GetDirectories(userdata))
            {
                var id3 = Path.GetFileName(dir);
                if (!Regex.IsMatch(id3, @"^\d+$"))
                {
                    continue; // not a Steam account folder
                }

                personaNames.TryGetValue(id3, out var name);

                accounts.Add(new SteamAccount
                {
                    SteamId3 = id3,
                    DisplayName = name,
                    ShortcutsVdfPath = Path.Combine(dir, "config", "shortcuts.vdf")
                });
            }

            return accounts;
        }

        public static bool IsSteamRunning() => Process.GetProcessesByName("steam").Length > 0;

        public static void RequestSteamShutdown(string steamPath)
        {
            var exe = Path.Combine(steamPath ?? string.Empty, "steam.exe");
            if (File.Exists(exe))
            {
                Process.Start(exe, "-shutdown");
            }
        }

        /// <summary>
        /// Best-effort friendly-name lookup from Steam's plain-text loginusers.vdf. This file uses
        /// regular (text) VDF, not the binary format shortcuts.vdf uses, so a light regex scan is
        /// enough here; if it fails for any reason we just fall back to showing the raw SteamID3.
        /// </summary>
        private static Dictionary<string, string> TryReadPersonaNames(string loginUsersVdfPath)
        {
            var result = new Dictionary<string, string>();
            try
            {
                if (!File.Exists(loginUsersVdfPath))
                {
                    return result;
                }

                var text = File.ReadAllText(loginUsersVdfPath);
                // Blocks look like:  "76561198000000000" { ... "PersonaName" "SomeName" ... }
                foreach (Match block in Regex.Matches(text, "\"(7656\\d{13})\"\\s*\\{([^}]*)\\}"))
                {
                    var steamId64 = block.Groups[1].Value;
                    var body = block.Groups[2].Value;
                    var nameMatch = Regex.Match(body, "\"PersonaName\"\\s*\"([^\"]*)\"");
                    if (!nameMatch.Success)
                    {
                        continue;
                    }

                    // SteamID3 (used for the userdata folder name) = low 32 bits of the 64-bit id.
                    if (ulong.TryParse(steamId64, out var id64))
                    {
                        var id3 = (uint)(id64 & 0xFFFFFFFF);
                        result[id3.ToString()] = nameMatch.Groups[1].Value;
                    }
                }
            }
            catch
            {
                // Non-critical: worst case the UI just shows SteamID3 folder names instead of nicknames.
            }
            return result;
        }
    }
}
