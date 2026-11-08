// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace bnetlauncher_setup
{
    /// <summary>
    /// Creates regular Windows .lnk shortcuts, for launching games without Steam.
    /// </summary>
    internal static class DesktopShortcuts
    {
        /// <returns>true if a new shortcut was created, false if an existing one was replaced.</returns>
        public static bool AddOrUpdate(string folder, string appName, string exePath, string startDir, string launchOptions)
        {
            var path = Path.Combine(folder, MakeFileName(appName) + ".lnk");
            var isNew = !File.Exists(path);

            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            try
            {
                dynamic shortcut = shell.CreateShortcut(path);
                try
                {
                    shortcut.TargetPath = exePath;
                    shortcut.Arguments = launchOptions;
                    shortcut.WorkingDirectory = startDir;
                    shortcut.IconLocation = exePath + ",0";
                    shortcut.Description = appName;
                    shortcut.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(shortcut);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }

            return isNew;
        }

        /// <summary>
        /// Game names can contain characters Windows doesn't allow in file names, such as
        /// the ':' and '|' in "Call of Duty: MWII | WZ2.0".
        /// </summary>
        internal static string MakeFileName(string appName)
        {
            var separators = Path.GetInvalidFileNameChars().Concat(new[] { ' ' }).ToArray();
            return string.Join(" ", appName.Split(separators, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
