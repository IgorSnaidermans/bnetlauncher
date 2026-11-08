// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.IO;
using Microsoft.Win32;

namespace bnetlauncher_setup
{
    internal static class BattleNetLocator
    {
        /// <summary>
        /// Best-effort detection of the Battle.net client's install folder, used as the
        /// suggested default place to drop bnetlauncher.exe. Returns null if not found,
        /// in which case the user just picks a folder themselves.
        /// </summary>
        public static string FindInstallPath()
        {
            var registryPath = TryFromRegistry();
            if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            {
                return registryPath;
            }

            foreach (var candidate in new[]
                     {
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Battle.net"),
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Battle.net"),
                     })
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string TryFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Blizzard Entertainment\Battle.net") ??
                                  Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blizzard Entertainment\Battle.net") ??
                                  Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Blizzard Entertainment\Battle.net"))
                {
                    return key?.GetValue("InstallPath") as string;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
