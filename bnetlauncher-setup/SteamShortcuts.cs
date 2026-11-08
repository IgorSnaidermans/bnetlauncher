// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Globalization;
using System.IO;

namespace bnetlauncher_setup
{
    /// <summary>
    /// Adds or updates a single non-Steam game shortcut inside a shortcuts.vdf file,
    /// leaving every other shortcut in the file untouched.
    /// </summary>
    internal static class SteamShortcuts
    {
        /// <summary>
        /// Saves a copy of the file as shortcuts.vdf.bnetlauncher-setup.bak next to it,
        /// replacing the backup from any previous install.
        /// </summary>
        public static void Backup(string shortcutsVdfPath)
        {
            if (File.Exists(shortcutsVdfPath))
            {
                File.Copy(shortcutsVdfPath, shortcutsVdfPath + ".bnetlauncher-setup.bak", overwrite: true);
            }
        }

        /// <summary>
        /// Adds a shortcut, or updates it in place if one with the same Exe and
        /// LaunchOptions already exists (so re-running the setup doesn't create duplicates).
        /// </summary>
        /// <returns>true if a new entry was added, false if an existing one was updated.</returns>
        public static bool AddOrUpdate(string shortcutsVdfPath, string appName, string exePath,
            string startDir, string launchOptions, string iconPath)
        {
            var root = VdfNode.Load(shortcutsVdfPath);
            var shortcuts = root.GetOrAddObject("shortcuts");

            VdfNode existing = null;
            var maxIndex = -1;

            foreach (var (key, type, value) in shortcuts.Children)
            {
                if (type != VdfNode.TypeObject)
                {
                    continue;
                }

                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) && idx > maxIndex)
                {
                    maxIndex = idx;
                }

                var entry = (VdfNode)value;
                if (string.Equals(entry.GetString("exe"), Quote(exePath), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entry.GetString("LaunchOptions"), launchOptions, StringComparison.Ordinal))
                {
                    existing = entry;
                }
            }

            var isNew = existing == null;
            var entryNode = existing ?? shortcuts.GetOrAddObject((maxIndex + 1).ToString(CultureInfo.InvariantCulture));

            var appId = unchecked((int)Crc32.GenerateShortcutAppId(exePath, appName));

            entryNode.SetInt32("appid", appId);
            // Note: Steam's own field-name casing is an inconsistent mix - "appid", "appname",
            // "exe" and "icon" are lowercase, everything else is PascalCase. Confirmed by
            // byte-comparing against real Steam-written shortcuts.vdf files; getting this wrong
            // doesn't corrupt the file, but risks Steam's own parser silently ignoring the field.
            entryNode.SetString("appname", appName);
            entryNode.SetString("exe", Quote(exePath));
            entryNode.SetString("StartDir", Quote(startDir));
            entryNode.SetString("icon", iconPath ?? string.Empty);
            entryNode.SetString("ShortcutPath", string.Empty);
            entryNode.SetString("LaunchOptions", launchOptions);
            entryNode.SetInt32("IsHidden", 0);
            entryNode.SetInt32("AllowDesktopConfig", 1);
            entryNode.SetInt32("AllowOverlay", 1);
            entryNode.SetInt32("OpenVR", 0);
            entryNode.SetInt32("Devkit", 0);
            entryNode.SetString("DevkitGameID", string.Empty);
            entryNode.SetInt32("DevkitOverrideAppID", 0);
            entryNode.SetInt32("LastPlayTime", 0);
            entryNode.SetString("FlatpakAppID", string.Empty);
            entryNode.GetOrAddObject("tags");

            root.Save(shortcutsVdfPath);
            return isNew;
        }

        private static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            return path.StartsWith("\"", StringComparison.Ordinal) ? path : $"\"{path}\"";
        }
    }
}
