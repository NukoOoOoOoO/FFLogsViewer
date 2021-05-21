﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;

namespace FFLogsViewer
{
    internal class PluginUi : IDisposable
    {
        private const float WindowHeight = 458;
        private const float ReducedWindowHeight = 88;
        private const float WindowWidth = 407;
        private readonly string[] _characterInput = new string[3];
        private readonly Configuration _configuration;
        private readonly Vector4 _defaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Dictionary<string, Vector4> _jobColors = new();
        private readonly Dictionary<string, Vector4> _logColors = new();
        private readonly Plugin _plugin;

        private float _bossesColumnWidth;

        private string _errorMessage = "";

        private bool _hasLoadingFailed;

        private bool _isLinkClicked;
        private float _jobsColumnWidth;
        private float _logsColumnWidth;
        private CharacterData _selectedCharacterData = new();
        private bool _settingsVisible;

        private bool _visible;

        public PluginUi(Plugin plugin, Configuration configuration)
        {
            _plugin = plugin;
            _configuration = configuration;

            _jobColors.Add("Astrologian", new Vector4(255.0f / 255.0f, 231.0f / 255.0f, 74.0f / 255.0f, 1.0f));
            _jobColors.Add("Bard", new Vector4(145.0f / 255.0f, 150.0f / 255.0f, 186.0f / 255.0f, 1.0f));
            _jobColors.Add("Black Mage", new Vector4(165.0f / 255.0f, 121.0f / 255.0f, 214.0f / 255.0f, 1.0f));
            _jobColors.Add("Dancer", new Vector4(226.0f / 255.0f, 176.0f / 255.0f, 175.0f / 255.0f, 1.0f));
            _jobColors.Add("Dark Knight", new Vector4(209.0f / 255.0f, 38.0f / 255.0f, 204.0f / 255.0f, 1.0f));
            _jobColors.Add("Dragoon", new Vector4(65.0f / 255.0f, 100.0f / 255.0f, 205.0f / 255.0f, 1.0f));
            _jobColors.Add("Gunbreaker", new Vector4(121.0f / 255.0f, 109.0f / 255.0f, 48.0f / 255.0f, 1.0f));
            _jobColors.Add("Machinist", new Vector4(110.0f / 255.0f, 225.0f / 255.0f, 214.0f / 255.0f, 1.0f));
            _jobColors.Add("Monk", new Vector4(214.0f / 255.0f, 156.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            _jobColors.Add("Ninja", new Vector4(175.0f / 255.0f, 25.0f / 255.0f, 100.0f / 255.0f, 1.0f));
            _jobColors.Add("Paladin", new Vector4(168.0f / 255.0f, 210.0f / 255.0f, 230.0f / 255.0f, 1.0f));
            _jobColors.Add("Red Mage", new Vector4(232.0f / 255.0f, 123.0f / 255.0f, 123.0f / 255.0f, 1.0f));
            _jobColors.Add("Samurai", new Vector4(228.0f / 255.0f, 109.0f / 255.0f, 4.0f / 255.0f, 1.0f));
            _jobColors.Add("Scholar", new Vector4(134.0f / 255.0f, 87.0f / 255.0f, 255.0f / 255.0f, 1.0f));
            _jobColors.Add("Summoner", new Vector4(45.0f / 255.0f, 155.0f / 255.0f, 120.0f / 255.0f, 1.0f));
            _jobColors.Add("Warrior", new Vector4(207.0f / 255.0f, 38.0f / 255.0f, 33.0f / 255.0f, 1.0f));
            _jobColors.Add("White Mage", new Vector4(255.0f / 255.0f, 240.0f / 255.0f, 220.0f / 255.0f, 1.0f));
            _jobColors.Add("Default", _defaultColor);

            _logColors.Add("Grey", new Vector4(102.0f / 255.0f, 102.0f / 255.0f, 102.0f / 255.0f, 1.0f));
            _logColors.Add("Green", new Vector4(30.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            _logColors.Add("Blue", new Vector4(0.0f / 255.0f, 112.0f / 255.0f, 255.0f / 255.0f, 1.0f));
            _logColors.Add("Magenta", new Vector4(163.0f / 255.0f, 53.0f / 255.0f, 238.0f / 255.0f, 1.0f));
            _logColors.Add("Orange", new Vector4(255.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            _logColors.Add("Pink", new Vector4(226.0f / 255.0f, 104.0f / 255.0f, 168.0f / 255.0f, 1.0f));
            _logColors.Add("Yellow", new Vector4(229.0f / 255.0f, 204.0f / 255.0f, 128.0f / 255.0f, 1.0f));
            _logColors.Add("Default", _defaultColor);
        }

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public bool SettingsVisible
        {
            get => _settingsVisible;
            set => _settingsVisible = value;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            DrawSettingsWindow();
            DrawMainWindow();
        }

        private void DrawSettingsWindow()
        {
            if (!SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("FF Logs Viewer Config", ref _settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse))
            {
                var configValue = _configuration.ButtonInContextMenu;
                if (ImGui.Checkbox("Add button in context menus", ref configValue))
                {
                    _plugin.ToggleContextMenuButton(configValue);
                    _configuration.ButtonInContextMenu = configValue;
                    _configuration.Save();
                }
            }

            ImGui.End();
        }

        private void DrawMainWindow()
        {
            if (!Visible) return;

            ImGui.SetNextWindowSize(new Vector2(WindowWidth, ReducedWindowHeight), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("FF Logs Viewer", ref _visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Columns(4, "InputColumns", true);

                var buttonsWidth = (ImGui.CalcTextSize("Target") + ImGui.CalcTextSize("Clipboard")).X + 40.0f;
                var colWidth = (ImGui.GetWindowWidth() - buttonsWidth) / 3.0f;
                var sizeMin = Math.Max(ImGui.CalcTextSize(_selectedCharacterData.FirstName).X,
                    Math.Max(ImGui.CalcTextSize(_selectedCharacterData.LastName).X,
                        ImGui.CalcTextSize(_selectedCharacterData.WorldName).X));
                var idealWindowWidth = sizeMin * 3 + buttonsWidth + 73.0f;
                if (idealWindowWidth < WindowWidth) idealWindowWidth = WindowWidth;
                float idealWindowHeight;
                if (_selectedCharacterData.IsEveryLogsReady && !_hasLoadingFailed)
                    idealWindowHeight = WindowHeight;
                else
                    idealWindowHeight = ReducedWindowHeight;
                ImGui.SetWindowSize(new Vector2(idealWindowWidth, idealWindowHeight));

                ImGui.SetColumnWidth(0, colWidth);
                ImGui.SetColumnWidth(1, colWidth);
                ImGui.SetColumnWidth(2, colWidth);
                ImGui.SetColumnWidth(3, buttonsWidth);

                ImGui.PushItemWidth(colWidth - 15);
                _characterInput[0] = _selectedCharacterData.FirstName;
                ImGui.InputTextWithHint("##FirstName", "First Name", ref _characterInput[0], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                _selectedCharacterData.FirstName = _characterInput[0];
                ImGui.PopItemWidth();

                ImGui.NextColumn();
                ImGui.PushItemWidth(colWidth - 15);
                _characterInput[1] = _selectedCharacterData.LastName;
                ImGui.InputTextWithHint("##LastName", "Last Name", ref _characterInput[1], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                _selectedCharacterData.LastName = _characterInput[1];
                ImGui.PopItemWidth();

                ImGui.NextColumn();
                ImGui.PushItemWidth(colWidth - 14);
                _characterInput[2] = _selectedCharacterData.WorldName;
                ImGui.InputTextWithHint("##WorldName", "World Name", ref _characterInput[2], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                _selectedCharacterData.WorldName = _characterInput[2];

                ImGui.PopItemWidth();

                ImGui.NextColumn();
                if (ImGui.Button("Clipboard"))
                    try
                    {
                        _selectedCharacterData = _plugin.GetClipboardCharacter();
                        _errorMessage = "";
                        _hasLoadingFailed = false;
                        try
                        {
                            _plugin.FetchLogs(_selectedCharacterData);
                        }
                        catch
                        {
                            _errorMessage = "World not supported or invalid.";
                        }
                    }
                    catch
                    {
                        _errorMessage = "No character found in the clipboard.";
                    }

                ImGui.SameLine();
                if (ImGui.Button("Target"))
                    try
                    {
                        _selectedCharacterData = _plugin.GetTargetCharacter();
                        _errorMessage = "";
                        _hasLoadingFailed = false;
                        try
                        {
                            _plugin.FetchLogs(_selectedCharacterData);
                        }
                        catch
                        {
                            _errorMessage = "World not supported or invalid.";
                        }
                    }
                    catch
                    {
                        _errorMessage = "Invalid target.";
                    }

                ImGui.Columns();

                ImGui.Separator();

                if (ImGui.Button("Clear"))
                {
                    _selectedCharacterData = new CharacterData();
                    _errorMessage = "";
                    _hasLoadingFailed = false;
                }

                ImGui.SameLine();
                if (_errorMessage == "")
                {
                    if (_selectedCharacterData.IsEveryLogsReady)
                    {
                        var nameVector =
                            ImGui.CalcTextSize(
                                $"Viewing logs of {_selectedCharacterData.LoadedFirstName} {_selectedCharacterData.LoadedLastName}@{_selectedCharacterData.LoadedWorldName}.");
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - nameVector.X / 2);
                        nameVector.X -= 7; // A bit too large on right side
                        nameVector.Y += 1;
                        ImGui.Selectable(
                            $"Viewing {_selectedCharacterData.LoadedFirstName} {_selectedCharacterData.LoadedLastName}@{_selectedCharacterData.LoadedWorldName}'s logs.",
                            ref _isLinkClicked, ImGuiSelectableFlags.None, nameVector);

                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to open on FF Logs.");

                        if (_isLinkClicked)
                        {
                            Process.Start(
                                $"https://www.fflogs.com/character/{_selectedCharacterData.RegionName}/{_selectedCharacterData.WorldName}/{_selectedCharacterData.FirstName} {_selectedCharacterData.LastName}");
                            _isLinkClicked = false;
                        }
                    }
                    else if (_selectedCharacterData.IsDataLoading)
                    {
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize("Loading...").X / 2);
                        ImGui.TextUnformatted("Loading...");
                    }
                    else
                    {
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize("Waiting...").X / 2);
                        ImGui.TextUnformatted("Waiting...");
                    }
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize(_errorMessage).X / 2);
                    ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), _errorMessage);
                }

                ImGui.SameLine();

                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Search").X * 1.5f + 4.0f);

                if (ImGui.Button("Search"))
                {
                    if (_selectedCharacterData.IsCharacterReady())
                    {
                        _errorMessage = "";
                        try
                        {
                            _plugin.FetchLogs(_selectedCharacterData);
                        }
                        catch
                        {
                            _errorMessage = "World not supported or invalid.";
                        }

                        _hasLoadingFailed = false;
                    }
                    else
                    {
                        _errorMessage = "One of the inputs is empty.";
                    }
                }

                if (_selectedCharacterData.IsEveryLogsReady && !_hasLoadingFailed)
                {
                    ImGui.Separator();

                    ImGui.Columns(5, "LogsDisplay", true);

                    _bossesColumnWidth =
                        ImGui.CalcTextSize("Cloud of Darkness").X + 17.5f; // Biggest text in first column
                    _jobsColumnWidth = ImGui.CalcTextSize("Dark Knight").X + 17.5f; // Biggest job name
                    _logsColumnWidth = (ImGui.GetWindowWidth() - _bossesColumnWidth - _jobsColumnWidth) / 3.0f;
                    ImGui.SetColumnWidth(0, _bossesColumnWidth);
                    ImGui.SetColumnWidth(1, _logsColumnWidth);
                    ImGui.SetColumnWidth(2, _logsColumnWidth);
                    ImGui.SetColumnWidth(3, _logsColumnWidth);
                    ImGui.SetColumnWidth(4, _jobsColumnWidth);

                    PrintTextColumn(1, "Eden's Promise");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Cloud of Darkness");
                    PrintTextColumn(1, "Shadowkeeper");
                    PrintTextColumn(1, "Fatebreaker");
                    PrintTextColumn(1, "Eden's Promise");
                    PrintTextColumn(1, "Oracle of Darkness");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Ultimates");
                    ImGui.Spacing();
                    PrintTextColumn(1, "TEA");
                    PrintTextColumn(1, "UwU");
                    PrintTextColumn(1, "UCoB");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Trials (Extreme)");
                    ImGui.Spacing();
                    PrintTextColumn(1, "The Emerald I");
                    PrintTextColumn(1, "The Emerald II");
                    PrintTextColumn(1, "The Diamond");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Unreal");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Leviathan");

                    try
                    {
                        ImGui.NextColumn();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Best, 2);

                        ImGui.NextColumn();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Median, 3);

                        ImGui.NextColumn();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Kills, 4);

                        ImGui.NextColumn();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Job, 5);

                        ImGui.Columns();
                    }
                    catch (Exception e)
                    {
                        _errorMessage = "Logs could not be loaded.";
                        PluginLog.LogError(e.Message);
                        PluginLog.LogError(e.StackTrace);
                        _hasLoadingFailed = true;
                    }
                }
            }

            ImGui.End();
        }

        private void PrintDataColumn(CharacterData.BossesId bossId, CharacterData.DataType dataType, int column)
        {
            string text;
            var color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            int log;
            switch (dataType)
            {
                case CharacterData.DataType.Best:
                    if (!_selectedCharacterData.Bests.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Best log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    color = GetLogColor(log);
                    break;

                case CharacterData.DataType.Median:
                    if (!_selectedCharacterData.Medians.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Median log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    color = GetLogColor(log);
                    break;
                case CharacterData.DataType.Kills:
                    if (!_selectedCharacterData.Kills.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Median log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    break;
                case CharacterData.DataType.Job:
                    if (!_selectedCharacterData.Jobs.TryGetValue((int) bossId, out var job))
                        throw new ArgumentNullException($"Job not found for boss ({bossId}).");
                    if (!_jobColors.TryGetValue(job, out color)) color = _jobColors["Default"];
                    text = job;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }


            PrintTextColumn(column, text, color);
        }

        private void PrintTextColumn(int column, string text, Vector4 color)
        {
            var position = column switch
            {
                1 => 8.0f,
                2 or 3 or 4 => _bossesColumnWidth + _logsColumnWidth / 2.0f + _logsColumnWidth * (column - 2) -
                               ImGui.CalcTextSize(text).X / 2.0f,
                _ => _bossesColumnWidth + _jobsColumnWidth / 2.0f + _logsColumnWidth * (column - 2) -
                     ImGui.CalcTextSize(text).X / 2.0f,
            };

            ImGui.SetCursorPosX(position);
            ImGui.TextColored(color, text);
        }

        private void PrintTextColumn(int column, string text)
        {
            PrintTextColumn(column, text, _defaultColor);
        }

        private Vector4 GetLogColor(int log)
        {
            return log switch
            {
                0 => _logColors["Default"],
                < 25 => _logColors["Grey"],
                < 50 => _logColors["Green"],
                < 75 => _logColors["Blue"],
                < 95 => _logColors["Magenta"],
                < 99 => _logColors["Orange"],
                99 => _logColors["Pink"],
                100 => _logColors["Yellow"],
                _ => _logColors["Default"],
            };
        }

        public void SetCharacter(CharacterData character)
        {
            _selectedCharacterData = character;
            _errorMessage = "";
            _hasLoadingFailed = false;
            try
            {
                _plugin.FetchLogs(_selectedCharacterData);
            }
            catch (Exception e)
            {
                _errorMessage = "World not supported or invalid.";
                PluginLog.LogError(e.Message);
            }
        }

        public void SetErrorMessage(string errorMessage)
        {
            _errorMessage = errorMessage;
        }
    }
}