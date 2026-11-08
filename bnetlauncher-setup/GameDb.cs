// Copyright (C) 2026 IgorSnaidermans
// This file is part of bnetlauncher.
//
// bnetlauncher is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections.Generic;
using System.IO;

namespace bnetlauncher_setup
{
    internal sealed class GameEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Client { get; set; }

        public override string ToString() => $"{Name}  [{Id}]";
    }

    /// <summary>
    /// A tiny reader for the same gamesdb.ini format the main launcher uses (simple
    /// "[id]" sections with "key=value" lines). Only the three fields the setup wizard
    /// needs, Id, Name and Client, are extracted; the rest is the launcher's business.
    /// </summary>
    internal static class GameDb
    {
        // gamesdb.ini also lists Epic Games Store titles (client=epic) bnetlauncher can start -
        // this wizard is specifically for adding Battle.net games as Steam shortcuts, so anything
        // else is filtered out rather than mixed into the same list.
        private static readonly string[] BattleNetClients = { "battlenet", "battlenet2" };

        /// <summary>
        /// Reads the game list compiled into this executable, so the setup works when it's
        /// downloaded on its own without a gamesdb.ini next to it.
        /// </summary>
        public static List<GameEntry> LoadBuiltIn()
        {
            using (var stream = typeof(GameDb).Assembly.GetManifestResourceStream("gamesdb.ini"))
            using (var reader = new StreamReader(stream))
            {
                return Parse(reader);
            }
        }

        private static List<GameEntry> Parse(TextReader reader)
        {
            var games = new List<GameEntry>();

            GameEntry current = null;
            string rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(";", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("[", System.StringComparison.Ordinal) && line.EndsWith("]", System.StringComparison.Ordinal))
                {
                    current = new GameEntry { Id = line.Substring(1, line.Length - 2).Trim() };
                    games.Add(current);
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (key == "name")
                {
                    current.Name = value;
                }
                else if (key == "client")
                {
                    current.Client = value;
                }
            }

            games.RemoveAll(g => string.IsNullOrEmpty(g.Name) ||
                                  System.Array.IndexOf(BattleNetClients, g.Client) < 0);
            return games;
        }
    }
}
