// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System;
using System.Linq;
using System.Windows.Forms;

namespace bnetlauncher_setup
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--diag", StringComparison.OrdinalIgnoreCase))
            {
                RunDiagnostics();
                return;
            }

            if (args.Length > 2 && args[0].Equals("--roundtrip", StringComparison.OrdinalIgnoreCase))
            {
                // Dev/test helper: load a shortcuts.vdf and re-save it verbatim to a different
                // path, so it can be byte-compared against the original without touching it.
                VdfNode.Load(args[1]).Save(args[2]);
                return;
            }

            if (args.Length > 1 && args[0].Equals("--list-shortcuts", StringComparison.OrdinalIgnoreCase))
            {
                if (!NativeAttachConsole())
                {
                    NativeAllocConsole();
                }

                var root = VdfNode.Load(args[1]);
                var shortcuts = root.GetOrAddObject("shortcuts");
                foreach (var (key, type, value) in shortcuts.Children)
                {
                    if (type != VdfNode.TypeObject)
                    {
                        continue;
                    }
                    var e = (VdfNode)value;
                    var name = e.GetString("appname") ?? e.GetString("AppName");
                    var exe = e.GetString("exe") ?? e.GetString("Exe");
                    Console.WriteLine($"[{key}] appname='{name}' exe={exe} LaunchOptions='{e.GetString("LaunchOptions")}'");
                }
                return;
            }

            if (args.Length > 2 && args[0].Equals("--remove-index", StringComparison.OrdinalIgnoreCase))
            {
                // Removes shortcut(s) by their exact index key (as --list-shortcuts prints them)
                // and re-numbers what's left, so the index keys stay contiguous.
                if (!NativeAttachConsole())
                {
                    NativeAllocConsole();
                }

                var vdfPath = args[1];
                var indicesToRemove = new System.Collections.Generic.HashSet<string>(args.Skip(2));

                var root = VdfNode.Load(vdfPath);
                var shortcuts = root.GetOrAddObject("shortcuts");

                var remaining = shortcuts.Children
                    .Where(c => c.Type != VdfNode.TypeObject || !indicesToRemove.Contains(c.Key))
                    .ToList();
                var removedCount = shortcuts.Children.Count(c => c.Type == VdfNode.TypeObject) -
                                    remaining.Count(c => c.Type == VdfNode.TypeObject);

                shortcuts.Children.Clear();
                var nextIndex = 0;
                foreach (var (key, type, value) in remaining)
                {
                    shortcuts.Children.Add(type == VdfNode.TypeObject
                        ? (nextIndex++.ToString(System.Globalization.CultureInfo.InvariantCulture), type, value)
                        : (key, type, value));
                }

                root.Save(vdfPath);
                Console.WriteLine($"Removed {removedCount} shortcut(s). {nextIndex} remain.");
                return;
            }

            if (args.Length > 2 && args[0].Equals("--remove-shortcut", StringComparison.OrdinalIgnoreCase))
            {
                // Removes a shortcut by its exact AppName and re-numbers the remaining entries
                // (0, 1, 2, ...) so the index keys stay contiguous, matching Steam's own convention.
                if (!NativeAttachConsole())
                {
                    NativeAllocConsole();
                }

                var vdfPath = args[1];
                var appNameToRemove = args[2];

                var root = VdfNode.Load(vdfPath);
                var shortcuts = root.GetOrAddObject("shortcuts");

                var toRemove = shortcuts.Children
                    .Where(c => c.Type == VdfNode.TypeObject &&
                                string.Equals(((VdfNode)c.Value).GetString("appname"), appNameToRemove, StringComparison.Ordinal))
                    .ToList();

                if (toRemove.Count == 0)
                {
                    Console.WriteLine($"No shortcut named '{appNameToRemove}' found.");
                    return;
                }

                foreach (var entry in toRemove)
                {
                    shortcuts.Children.Remove(entry);
                }

                var remaining = shortcuts.Children.Where(c => c.Type == VdfNode.TypeObject).ToList();
                shortcuts.Children.RemoveAll(c => c.Type == VdfNode.TypeObject);
                for (var i = 0; i < remaining.Count; i++)
                {
                    shortcuts.Children.Add((i.ToString(System.Globalization.CultureInfo.InvariantCulture), VdfNode.TypeObject, remaining[i].Value));
                }

                root.Save(vdfPath);
                Console.WriteLine($"Removed {toRemove.Count} shortcut(s) named '{appNameToRemove}'. {remaining.Count} remain.");
                return;
            }

            if (args.Length > 4 && args[0].Equals("--live-install", StringComparison.OrdinalIgnoreCase))
            {
                // Headless equivalent of clicking "Install" for one game / one Steam account.
                // Exercises the exact same SteamShortcuts.AddOrUpdate() call MainForm uses.
                if (!NativeAttachConsole())
                {
                    NativeAllocConsole();
                }

                var sourceExe = args[1];
                var targetFolder = args[2];
                var gameId = args[3];
                var steamId3 = args[4];

                var games = GameDb.LoadBuiltIn();
                var game = games.Find(g => g.Id == gameId);
                if (game == null)
                {
                    Console.WriteLine($"Unknown game id '{gameId}'.");
                    return;
                }

                var steamPath = SteamLocator.FindSteamInstallPath();
                var account = SteamLocator.FindAccounts(steamPath).Find(a => a.SteamId3 == steamId3);
                if (account == null)
                {
                    Console.WriteLine($"Steam account '{steamId3}' not found.");
                    return;
                }

                System.IO.Directory.CreateDirectory(targetFolder);
                var targetExe = System.IO.Path.Combine(targetFolder, "bnetlauncher.exe");
                System.IO.File.Copy(sourceExe, targetExe, overwrite: true);
                Console.WriteLine($"Copied '{sourceExe}' -> '{targetExe}'.");

                var added = SteamShortcuts.AddOrUpdate(account.ShortcutsVdfPath, game.Name, targetExe,
                    targetFolder, game.Id, targetExe);
                Console.WriteLine($"{(added ? "Added" : "Updated")} shortcut for '{game.Name}' ({game.Id}) " +
                                   $"in '{account.ShortcutsVdfPath}'.");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// Read-only detection report, handy to ask for when someone opens a "setup doesn't
        /// find my Steam/Battle.net" issue. Prints to the console, doesn't write anything.
        /// </summary>
        private static void RunDiagnostics()
        {
            if (!NativeAttachConsole())
            {
                NativeAllocConsole();
            }

            Console.WriteLine("bnetlauncher-setup diagnostics");
            Console.WriteLine("===============================");

            var battleNet = BattleNetLocator.FindInstallPath();
            Console.WriteLine($"Battle.net install path: {battleNet ?? "(not found)"}");

            var steamPath = SteamLocator.FindSteamInstallPath();
            Console.WriteLine($"Steam install path:      {steamPath ?? "(not found)"}");
            Console.WriteLine($"Steam running:            {SteamLocator.IsSteamRunning()}");

            var accounts = SteamLocator.FindAccounts(steamPath);
            Console.WriteLine($"Steam accounts found:     {accounts.Count}");
            foreach (var a in accounts)
            {
                Console.WriteLine($"  - {a.SteamId3}{(a.DisplayName != null ? $" ({a.DisplayName})" : "")}");
                Console.WriteLine($"    shortcuts.vdf exists: {System.IO.File.Exists(a.ShortcutsVdfPath)}");
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        private static bool NativeAttachConsole() => AttachConsole(-1);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private static bool NativeAllocConsole() => AllocConsole();
    }
}
