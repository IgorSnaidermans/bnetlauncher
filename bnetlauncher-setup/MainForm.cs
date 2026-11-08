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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace bnetlauncher_setup
{
    internal sealed class MainForm : Form
    {
        private readonly CheckedListBox _gamesList = new CheckedListBox();
        private readonly RadioButton _battleNetRadio = new RadioButton();
        private readonly RadioButton _customFolderRadio = new RadioButton();
        private readonly TextBox _customFolderBox = new TextBox();
        private readonly Button _browseFolderButton = new Button();
        private readonly TextBox _sourceExeBox = new TextBox();
        private readonly Button _browseExeButton = new Button();
        private readonly CheckBox _steamCheck = new CheckBox();
        private readonly CheckBox _desktopCheck = new CheckBox();
        private readonly CheckedListBox _accountsList = new CheckedListBox();
        private readonly Label _accountsHint = new Label();
        private readonly Button _installButton = new Button();
        private readonly Label _statusLabel = new Label();
        private readonly Button _saveLogButton = new Button();
        private readonly LinkLabel _issueLinkLabel = new LinkLabel();

        private const string IssuesUrl = "https://github.com/IgorSnaidermans/bnetlauncher/issues";

        // Full step-by-step detail, for "Save log..." - the UI itself only ever shows one short
        // status line, per feedback that a big always-visible log window was overkill.
        private readonly List<string> _logLines = new List<string>();

        private readonly string _steamInstallPath;
        private readonly List<SteamAccount> _accounts;
        private readonly string _battleNetInstallPath;
        private readonly string _desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        public MainForm()
        {
            // This baseline must be set explicitly - without it AutoScaleMode.Dpi has nothing to
            // scale from, so fonts grow with Windows display scaling but every hardcoded pixel
            // size below (GroupBox heights, label heights, ...) stays put and text overlaps.
            // Same class of bug as the Play-button click fix in the main launcher.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "bnetlauncher setup";
            StartPosition = FormStartPosition.CenterScreen;

            // .NET Framework WinForms' own "resize the form to match DPI" auto-scaling turned out
            // not to actually grow the WINDOW on a 225%-scaled display in testing: fonts and
            // controls render at the correct (bigger) DPI-scaled size, but the outer form stays at
            // its literal design-time pixel size, so scaled-up content no longer fits and clips at
            // the edge. Sizing the window off the same DPI scale factor the fonts already use
            // fixes that directly, instead of fighting AutoScaleMode.Dpi's window-sizing behavior.
            float dpiScale;
            using (var g = CreateGraphics())
            {
                dpiScale = g.DpiX / 96f;
            }

            var workArea = Screen.PrimaryScreen.WorkingArea;
            Width = Math.Min((int)(1040 * dpiScale), workArea.Width - 40);
            Height = Math.Min((int)(680 * dpiScale), workArea.Height - 40);
            MinimumSize = new Size((int)(900 * dpiScale), (int)(560 * dpiScale));

            _steamInstallPath = SteamLocator.FindSteamInstallPath();
            _accounts = SteamLocator.FindAccounts(_steamInstallPath);
            _battleNetInstallPath = BattleNetLocator.FindInstallPath();

            BuildLayout();
            PopulateGames();
            PopulateAccounts();
            PrefillSourceExe();
        }

        private void BuildLayout()
        {
            // Overall shape: an intro line, then a wide two-column body (settings on the left,
            // games on the right), then a compact status line at the bottom instead of a log panel.
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 3,
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            var intro = new Label
            {
                Text = "Pick your games, where to put bnetlauncher.exe, and where to create shortcuts. " +
                       "The Steam overlay only works with Steam shortcuts.",
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 8),
            };
            root.Controls.Add(intro, 0, 0);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.Controls.Add(body, 0, 1);

            body.Controls.Add(BuildSettingsColumn(), 0, 0);

            var gamesGroup = new GroupBox { Text = "Games", Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0) };
            _gamesList.Dock = DockStyle.Fill;
            _gamesList.CheckOnClick = true;
            gamesGroup.Controls.Add(_gamesList);
            body.Controls.Add(gamesGroup, 1, 0);

            root.Controls.Add(BuildStatusBar(), 0, 2);
        }

        /// <summary>
        /// A single-line status area: a spinner while an install is running, one short result
        /// message afterwards, a button to save the full detail to a file, and - only shown when
        /// something went wrong - a link to report it.
        /// </summary>
        private Control BuildStatusBar()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 8, 0, 0),
            };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
            };

            _statusLabel.AutoSize = true;
            _statusLabel.Margin = new Padding(3, 8, 10, 3);
            row.Controls.Add(_statusLabel);

            _saveLogButton.Text = "Save log...";
            _saveLogButton.AutoSize = true;
            _saveLogButton.Margin = new Padding(3, 3, 3, 3);
            _saveLogButton.Visible = false;
            _saveLogButton.Click += SaveLogButton_Click;
            row.Controls.Add(_saveLogButton);

            panel.Controls.Add(row, 0, 0);

            _issueLinkLabel.AutoSize = true;
            _issueLinkLabel.Margin = new Padding(3, 0, 3, 0);
            _issueLinkLabel.Text = $"Something went wrong - please save the log above and report it here: {IssuesUrl}";
            _issueLinkLabel.Visible = false;
            _issueLinkLabel.LinkClicked += (s, e) => Process.Start(IssuesUrl);
            panel.Controls.Add(_issueLinkLabel, 0, 1);

            return panel;
        }

        /// <summary>
        /// The right-hand column: install folder, source exe, Steam accounts, then the Install
        /// button pinned to the bottom. Every group is left to size itself (AutoSize) instead of
        /// a hardcoded pixel height, so it still fits its text under Windows display scaling.
        /// </summary>
        private Control BuildSettingsColumn()
        {
            var column = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
            };
            column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            column.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Empty spacer that absorbs any leftover height. Without it, with every row above set
            // to AutoSize, the TableLayoutPanel stretched all of them evenly and the Install
            // button ended up floating in the middle of the column instead of right under the
            // accounts box.
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            column.Controls.Add(BuildInstallFolderGroup(), 0, 0);
            column.Controls.Add(BuildSourceExeGroup(), 0, 1);

            var accountsGroup = new GroupBox
            {
                Text = "Create shortcuts",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 4, 8, 8),
                Margin = new Padding(0, 8, 0, 8),
            };
            var accountsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RowCount = 4,
                ColumnCount = 1,
            };
            accountsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            accountsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            accountsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            accountsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _steamCheck.Text = "Steam, for these accounts:";
            _steamCheck.AutoSize = true;
            _steamCheck.CheckedChanged += (s, e) => _accountsList.Enabled = _steamCheck.Checked;
            accountsPanel.Controls.Add(_steamCheck, 0, 0);
            _accountsHint.Dock = DockStyle.Top;
            _accountsHint.AutoSize = true;
            _accountsHint.ForeColor = Color.DarkRed;
            accountsPanel.Controls.Add(_accountsHint, 0, 1);
            // Fixed, compact height (enough for ~3 accounts) instead of filling the column - most
            // people only have one or two Steam accounts on a PC, so a huge mostly-empty box was
            // just wasted space.
            _accountsList.Dock = DockStyle.Top;
            _accountsList.Height = 78;
            _accountsList.CheckOnClick = true;
            accountsPanel.Controls.Add(_accountsList, 0, 2);
            _desktopCheck.Text = "Desktop";
            _desktopCheck.AutoSize = true;
            _desktopCheck.Margin = new Padding(3, 6, 3, 0);
            accountsPanel.Controls.Add(_desktopCheck, 0, 3);
            accountsGroup.Controls.Add(accountsPanel);
            column.Controls.Add(accountsGroup, 0, 2);

            _installButton.Text = "Install";
            _installButton.AutoSize = true;
            _installButton.Padding = new Padding(8);
            _installButton.Dock = DockStyle.Fill;
            _installButton.Click += InstallButton_Click;
            column.Controls.Add(_installButton, 0, 3);

            return column;
        }

        private Control BuildInstallFolderGroup()
        {
            var group = new GroupBox
            {
                Text = "Install bnetlauncher.exe to",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 4, 8, 10),
            };
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 2,
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // The full install path can be long, so it goes on its own indented detail line
            // instead of inside the radio button's label, which doesn't wrap and would clip.
            _battleNetRadio.Text = "Battle.net folder";
            _battleNetRadio.AutoSize = true;
            _battleNetRadio.Margin = new Padding(3, 4, 3, 0);
            _battleNetRadio.Enabled = _battleNetInstallPath != null;
            _battleNetRadio.Checked = _battleNetInstallPath != null;
            panel.Controls.Add(_battleNetRadio, 0, 0);
            panel.SetColumnSpan(_battleNetRadio, 3);

            var battleNetDetail = new Label
            {
                Text = _battleNetInstallPath ?? "(not detected - pick a custom folder below)",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(22, 0, 3, 6),
            };
            panel.Controls.Add(battleNetDetail, 0, 1);
            panel.SetColumnSpan(battleNetDetail, 3);

            _customFolderRadio.Text = "Custom folder:";
            _customFolderRadio.AutoSize = true;
            _customFolderRadio.Margin = new Padding(3, 4, 3, 4);
            _customFolderRadio.Checked = _battleNetInstallPath == null;
            panel.Controls.Add(_customFolderRadio, 0, 2);

            _customFolderBox.Dock = DockStyle.Fill;
            _customFolderBox.Margin = new Padding(3, 4, 3, 4);
            panel.Controls.Add(_customFolderBox, 1, 2);

            _browseFolderButton.Text = "Browse...";
            _browseFolderButton.AutoSize = true;
            _browseFolderButton.Margin = new Padding(6, 4, 3, 4);
            _browseFolderButton.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _customFolderBox.Text = dlg.SelectedPath;
                        _customFolderRadio.Checked = true;
                    }
                }
            };
            panel.Controls.Add(_browseFolderButton, 2, 2);

            group.Controls.Add(panel);
            return group;
        }

        private Control BuildSourceExeGroup()
        {
            var group = new GroupBox
            {
                Text = "Source bnetlauncher.exe",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 4, 8, 8),
                Margin = new Padding(0, 8, 0, 0),
            };
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _sourceExeBox.Dock = DockStyle.Fill;
            _sourceExeBox.Margin = new Padding(3, 4, 3, 4);
            panel.Controls.Add(_sourceExeBox, 0, 0);

            _browseExeButton.Text = "Browse...";
            _browseExeButton.AutoSize = true;
            _browseExeButton.Margin = new Padding(6, 4, 3, 4);
            _browseExeButton.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "bnetlauncher.exe|bnetlauncher.exe|All files|*.*" })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _sourceExeBox.Text = dlg.FileName;
                    }
                }
            };
            panel.Controls.Add(_browseExeButton, 1, 0);

            group.Controls.Add(panel);
            return group;
        }

        private void PopulateGames()
        {
            foreach (var g in GameDb.LoadBuiltIn().OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
            {
                _gamesList.Items.Add(g);
            }
        }

        private void PopulateAccounts()
        {
            if (_accounts.Count == 0)
            {
                _accountsHint.Text = "No Steam accounts were detected on this PC.";
                _accountsList.Enabled = false;
                _steamCheck.Enabled = false;
                _desktopCheck.Checked = true;
                return;
            }

            _steamCheck.Checked = true;
            _accountsHint.Text = string.Empty;
            foreach (var a in _accounts)
            {
                var index = _accountsList.Items.Add(a);
                _accountsList.SetItemChecked(index, true);
            }
        }

        private void PrefillSourceExe()
        {
            var candidate = Path.Combine(Application.StartupPath, "bnetlauncher.exe");
            _sourceExeBox.Text = File.Exists(candidate) ? candidate : string.Empty;
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            var selectedGames = _gamesList.CheckedItems.Cast<GameEntry>().ToList();
            if (selectedGames.Count == 0)
            {
                MessageBox.Show(this, "Pick at least one game first.", "Nothing selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sourceExe = _sourceExeBox.Text.Trim();
            if (string.IsNullOrEmpty(sourceExe) || !File.Exists(sourceExe))
            {
                MessageBox.Show(this, "Point to a valid bnetlauncher.exe first.", "Missing bnetlauncher.exe",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var targetFolder = _battleNetRadio.Checked ? _battleNetInstallPath : _customFolderBox.Text.Trim();
            if (string.IsNullOrEmpty(targetFolder))
            {
                MessageBox.Show(this, "Pick a folder to install bnetlauncher.exe into.", "Missing folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _installButton.Enabled = false;
            _logLines.Clear();
            _saveLogButton.Visible = false;
            _issueLinkLabel.Visible = false;
            _statusLabel.Text = string.Empty;
            // The work below is almost always near-instant (a file copy + a couple of small file
            // writes); a spinner control would just flash. The one exception is waiting for Steam
            // to close, which already pumps the message loop every 250ms in WaitForSteamToExit,
            // so the wait cursor stays visible throughout that too.
            Cursor = Cursors.WaitCursor;
            Application.DoEvents(); // let the disabled button/cursor repaint before the work below

            try
            {
                Install(selectedGames, sourceExe, targetFolder);
            }
            finally
            {
                Cursor = Cursors.Default;
                _installButton.Enabled = true;
                _saveLogButton.Visible = _logLines.Count > 0;
            }
        }

        private void Install(List<GameEntry> selectedGames, string sourceExe, string targetFolder)
        {
            string targetExe;
            try
            {
                Directory.CreateDirectory(targetFolder);
                targetExe = Path.Combine(targetFolder, "bnetlauncher.exe");
                // File.Copy onto itself throws, and running the setup from the install folder is normal.
                if (string.Equals(Path.GetFullPath(sourceExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
                {
                    Log($"bnetlauncher.exe is already at '{targetExe}'.");
                }
                else
                {
                    File.Copy(sourceExe, targetExe, overwrite: true);
                    Log($"Copied bnetlauncher.exe to '{targetExe}'.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                var message = $"Couldn't write to '{targetFolder}' (access denied). Pick a folder you have " +
                               "write access to, or run this tool as Administrator.";
                Log(message);
                SetStatus(message, isError: true);
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is NotSupportedException)
            {
                Log($"Couldn't copy bnetlauncher.exe: {ex.Message}");
                SetStatus($"Couldn't copy bnetlauncher.exe: {ex.Message}", isError: true);
                return;
            }

            var selectedAccounts = _steamCheck.Checked
                ? _accountsList.CheckedItems.Cast<SteamAccount>().ToList()
                : new List<SteamAccount>();
            var addToDesktop = _desktopCheck.Checked;

            if (selectedAccounts.Count == 0 && !addToDesktop)
            {
                Log("No shortcut destination selected, so no shortcuts were added.");
                SetStatus("bnetlauncher.exe installed. No shortcuts added (no destination selected).", isError: false);
                return;
            }

            var added = 0;
            var updated = 0;
            var failed = 0;

            if (addToDesktop)
            {
                foreach (var game in selectedGames)
                {
                    try
                    {
                        var wasAdded = DesktopShortcuts.AddOrUpdate(_desktopFolder, game.Name, targetExe, targetFolder, game.Id);

                        if (wasAdded) added++; else updated++;
                        Log($"[Desktop] {(wasAdded ? "Added" : "Updated")} shortcut for '{game.Name}' ({game.Id}).");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Log($"[Desktop] Failed to add '{game.Name}': {ex.Message}");
                    }
                }
            }

            var steamSkipped = selectedAccounts.Count > 0 && !EnsureSteamClosed();
            if (steamSkipped)
            {
                Log("Steam is still running, so no Steam shortcuts were added.");
                selectedAccounts.Clear();
            }

            foreach (var account in selectedAccounts)
            {
                try
                {
                    SteamShortcuts.Backup(account.ShortcutsVdfPath);
                }
                catch (Exception ex)
                {
                    failed += selectedGames.Count;
                    Log($"[{account}] Skipped: couldn't back up shortcuts.vdf: {ex.Message}");
                    continue;
                }

                foreach (var game in selectedGames)
                {
                    try
                    {
                        var wasAdded = SteamShortcuts.AddOrUpdate(
                            account.ShortcutsVdfPath,
                            appName: game.Name,
                            exePath: targetExe,
                            startDir: targetFolder,
                            launchOptions: game.Id,
                            iconPath: targetExe);

                        if (wasAdded) added++; else updated++;
                        Log($"[{account}] {(wasAdded ? "Added" : "Updated")} shortcut for '{game.Name}' ({game.Id}).");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Log($"[{account}] Failed to add '{game.Name}': {ex.Message}");
                    }
                }
            }

            Log(string.Empty);
            var steamHint = selectedAccounts.Count > 0 ? " Start (or restart) Steam to see them." : string.Empty;
            if (failed > 0)
            {
                Log("Done, with errors." + steamHint);
                SetStatus($"{failed} shortcut(s) failed ({added} added, {updated} updated). See the log for details.",
                    isError: true);
            }
            else if (steamSkipped)
            {
                SetStatus(addToDesktop
                        ? $"Desktop shortcuts done ({added} added, {updated} updated). Steam skipped: close Steam and click Install again."
                        : "bnetlauncher.exe installed, but no Steam shortcuts were added. Close Steam and click Install again.",
                    isError: false);
            }
            else
            {
                Log("Done." + steamHint);
                SetStatus($"Done - {added} shortcut(s) added, {updated} updated." + steamHint, isError: false);
            }
        }

        /// <summary>
        /// Steam rewrites shortcuts.vdf when it exits, so anything written while it runs would be lost.
        /// </summary>
        /// <returns>true if Steam isn't running, either already or after closing it.</returns>
        private bool EnsureSteamClosed()
        {
            if (!SteamLocator.IsSteamRunning())
            {
                return true;
            }

            var result = MessageBox.Show(this,
                "Steam is currently running. It needs to be closed while shortcuts are added, otherwise it " +
                "will overwrite these changes when it exits.\n\nClose Steam now?",
                "Steam is running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Log("Closing Steam...");
                SteamLocator.RequestSteamShutdown(_steamInstallPath);
                WaitForSteamToExit();
            }

            return !SteamLocator.IsSteamRunning();
        }

        private void WaitForSteamToExit()
        {
            const int maxWaitMs = 30000;
            var waited = 0;
            while (SteamLocator.IsSteamRunning() && waited < maxWaitMs)
            {
                System.Threading.Thread.Sleep(250);
                Application.DoEvents();
                waited += 250;
            }

            if (!SteamLocator.IsSteamRunning())
            {
                Log("Steam is closed.");
            }
        }

        private void SetStatus(string message, bool isError)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = isError ? Color.DarkRed : Color.DarkGreen;
            _issueLinkLabel.Visible = isError;
            _saveLogButton.Visible = _logLines.Count > 0;
        }

        private void SaveLogButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
                   {
                       Filter = "Text file|*.txt",
                       FileName = "bnetlauncher-setup-log.txt",
                   })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllLines(dlg.FileName, _logLines);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Couldn't save the log: {ex.Message}", "Save log",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Log(string message)
        {
            _logLines.Add(message);
        }
    }
}
