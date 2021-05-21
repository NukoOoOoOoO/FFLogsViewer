﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace FFLogsViewer
{
    public class Plugin : IDalamudPlugin
    {
        private const string CommandName = "/fflogs";

        private readonly string[] _eu =
        {
            "Cerberus", "Louisoix", "Moogle", "Omega", "Ragnarok", "Spriggan",
            "Lich", "Odin", "Phoenix", "Shiva", "Zodiark", "Twintania",
        };

        private readonly string[] _jp =
        {
            "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Ramuh", "Tonberry", "Typhon", "Unicorn",
            "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima", "Valefor", "Yojimbo",
            "Zeromus",
            "Anima", "Asura", "Belias", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium",
            "Shinryu", "Titan",
        };

        private readonly string[] _na =
        {
            "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren",
            "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros",
            "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera",
        };

        private Configuration _configuration;
        private FfLogsClient.Token _token;
        private PluginUi _ui;

        public DalamudPluginInterface Pi;
        public XivCommonBase Common { get; private set; }
        private ContextMenu ContextMenu { get; set; }

        public string Name => "FF Logs Viewer";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Pi = pluginInterface;

            _configuration = Pi.GetPluginConfig() as Configuration ?? new Configuration();
            _configuration.Initialize(Pi);

            _ui = new PluginUi(this, _configuration);

            if (_configuration.ButtonInContextMenu)
            {
                Common = new XivCommonBase(Pi, Hooks.PartyFinder | Hooks.ContextMenu);
                ContextMenu = new ContextMenu(this);
            }

            Pi.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the FF Logs viewer window or parse the arguments for a character.",
                ShowInHelp = true,
            });

            Pi.UiBuilder.OnBuildUi += DrawUi;
            Pi.UiBuilder.OnOpenConfigUi += (_, _) => ToggleSettingsUi();

            Task.Run(async () =>
            {
                _token = await FfLogsClient.GetToken(_configuration.ClientId, _configuration.ClientSecret)
                    .ConfigureAwait(false);
            });
        }

        public void Dispose()
        {
            Common?.Dispose();
            ContextMenu?.Dispose();
            _ui.Dispose();
            Pi.CommandManager.RemoveHandler(CommandName);
            Pi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            if (string.IsNullOrEmpty(args))
                _ui.Visible = !_ui.Visible;
            else if (args.Equals("config", StringComparison.OrdinalIgnoreCase))
                _ui.SettingsVisible = !_ui.SettingsVisible;
            else
                SearchPlayer(args);
        }

        private void DrawUi()
        {
            _ui.Draw();
        }

        private void ToggleSettingsUi()
        {
            _ui.SettingsVisible = !_ui.SettingsVisible;
        }

        public void ToggleContextMenuButton(bool enable)
        {
            switch (enable)
            {
                case true when ContextMenu != null:
                case false when ContextMenu == null:
                    return;
                case true:
                    Common = new XivCommonBase(Pi, Hooks.PartyFinder | Hooks.ContextMenu);
                    ContextMenu = new ContextMenu(this);
                    break;
                default:
                    Common?.Dispose();
                    ContextMenu?.Dispose();
                    break;
            }
        }

        public void SearchPlayer(string args)
        {
            try
            {
                _ui.Visible = true;
                _ui.SetCharacter(ParseTextForChar(args));
            }
            catch
            {
                _ui.SetErrorMessage("Character could not be found.");
            }
        }

        private static CharacterData GetPlayerData(PlayerCharacter playerCharacter)
        {
            return new()
            {
                FirstName = playerCharacter.Name.Split(' ')[0],
                LastName = playerCharacter.Name.Split(' ')[1],
                WorldName = playerCharacter.HomeWorld.GameData.Name,
            };
        }

        public CharacterData GetTargetCharacter()
        {
            var target = Pi.ClientState.Targets.CurrentTarget;
            if (target is PlayerCharacter targetCharacter && target.ObjectKind != ObjectKind.Companion)
                return GetPlayerData(targetCharacter);

            throw new ArgumentException("Not a valid target.");
        }

        private bool IsWorldValid(string worldAttempt)
        {
            var world = Pi.Data.GetExcelSheet<World>()
                .FirstOrDefault(
                    x => x.Name.ToString().Equals(worldAttempt, StringComparison.InvariantCultureIgnoreCase));

            return world != null;
        }

        public CharacterData GetClipboardCharacter()
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text)) throw new ArgumentException("Invalid clipboard.");

            var clipboardRawText = Clipboard.GetText(TextDataFormat.Text);
            return ParseTextForChar(clipboardRawText);
        }

        private CharacterData ParseTextForChar(string rawText)
        {
            var character = new CharacterData();
            rawText = rawText.Replace("'s party for", " ");

            rawText = rawText.Replace("You join", " ");
            rawText = Regex.Replace(rawText, "\\[.*?\\]", " ");
            rawText = Regex.Replace(rawText, "[^A-Za-z '-]", " ");
            rawText = string.Concat(rawText.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            rawText = Regex.Replace(rawText, @"\s+", " ");

            var words = rawText.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            var index = -1;
            for (var i = 0; index == -1 && i < _na.Length; i++) index = Array.IndexOf(words, _na[i]);
            for (var i = 0; index == -1 && i < _eu.Length; i++) index = Array.IndexOf(words, _eu[i]);
            for (var i = 0; index == -1 && i < _jp.Length; i++) index = Array.IndexOf(words, _jp[i]);

            if (index - 2 >= 0)
            {
                character.FirstName = words[index - 2];
                character.LastName = words[index - 1];
                character.WorldName = words[index];
            }
            else if (words.Length >= 2)
            {
                character.FirstName = words[0];
                character.LastName = words[1];
                character.WorldName = Pi.ClientState.LocalPlayer.HomeWorld.GameData.Name;
            }
            else
            {
                throw new ArgumentException("Invalid text.");
            }

            if (!char.IsUpper(character.FirstName[0]) || !char.IsUpper(character.LastName[0]))
                throw new ArgumentException("Invalid text.");

            return character;
        }

        public void FetchLogs(CharacterData characterData)
        {
            if (characterData.IsEveryLogsReady) characterData.ResetLogs();

            characterData.RegionName = GetRegionName(characterData.WorldName);

            characterData.IsDataLoading = true;
            Task.Run(async () =>
            {
                var logData = await FfLogsClient.GetLogsData(characterData, _token).ConfigureAwait(false);
                if (logData?.data?.characterData?.character == null)
                {
                    if (logData?.errors != null)
                    {
                        characterData.IsDataLoading = false;
                        _ui.SetErrorMessage("Malformed GraphQL query.");
                        PluginLog.Log($"Malformed GraphQL query: {logData}");
                        return;
                    }

                    if (logData == null)
                    {
                        characterData.IsDataLoading = false;
                        _ui.SetErrorMessage("Could not reach FF Logs servers.");
                        PluginLog.Log("Could not reach FF Logs servers.");
                        return;
                    }

                    characterData.IsDataLoading = false;
                    _ui.SetErrorMessage("Character not found.");
                    return;
                }

                try
                {
                    if (logData.data.characterData.character.hidden == "true")
                    {
                        characterData.IsDataLoading = false;
                        _ui.SetErrorMessage(
                            $"{characterData.FirstName} {characterData.LastName}@{characterData.WorldName}'s logs are hidden.");
                        return;
                    }

                    ParseLogs(characterData, logData.data.characterData.character.EdenPromise);
                    ParseLogs(characterData, logData.data.characterData.character.EdenVerse);
                    ParseLogs(characterData, logData.data.characterData.character.UltimatesShB);
                    ParseLogs(characterData, logData.data.characterData.character.UltimatesSB);
                    ParseLogs(characterData, logData.data.characterData.character.ExtremesII);
                    ParseLogs(characterData, logData.data.characterData.character.ExtremesIII);
                    ParseLogs(characterData, logData.data.characterData.character.Unreal);
                }
                catch (Exception e)
                {
                    characterData.IsDataLoading = false;
                    _ui.SetErrorMessage("Could not load data from FF Logs servers.");
                    PluginLog.LogError(e.Message);
                    PluginLog.LogError(e.StackTrace);
                    return;
                }

                characterData.IsEveryLogsReady = true;
                characterData.LoadedFirstName = characterData.FirstName;
                characterData.LoadedLastName = characterData.LastName;
                characterData.LoadedWorldName = characterData.WorldName;
                characterData.IsDataLoading = false;
            }).ContinueWith(t =>
            {
                if (!t.IsFaulted) return;
                characterData.IsDataLoading = false;
                _ui.SetErrorMessage("Networking error, please try again.");
                if (t.Exception == null) return;
                foreach (var e in t.Exception.Flatten().InnerExceptions)
                {
                    PluginLog.LogError(e.Message);
                    PluginLog.LogError(e.StackTrace);
                }
            });
        }

        private string GetRegionName(string worldName)
        {
            if (!IsWorldValid(worldName)) throw new ArgumentException("Invalid world.");

            if (_na.Any(worldName.Contains)) return "NA";

            if (_eu.Any(worldName.Contains)) return "EU";

            if (_jp.Any(worldName.Contains)) return "JP";

            throw new ArgumentException("World not supported.");
        }

        private static void ParseLogs(CharacterData characterData, dynamic zone)
        {
            if (zone?.rankings == null || zone.rankings.Count == 0)
                throw new InvalidOperationException("Field zone.rankings not found or empty.");
            foreach (var fight in zone.rankings)
            {
                if (fight?.encounter == null) throw new InvalidOperationException("Field fight.encounter not found.");

                int bossId = fight.encounter.id;
                int best;
                int median;
                int kills;
                string job;
                if (fight.spec == null)
                {
                    best = 0;
                    median = 0;
                    kills = 0;
                    job = "-";
                }
                else
                {
                    best = Convert.ToInt32(Math.Floor((double) fight.rankPercent));
                    median = Convert.ToInt32(Math.Floor((double) fight.medianPercent));
                    kills = Convert.ToInt32(Math.Floor((double) fight.totalKills));
                    job = Regex.Replace(fight.spec.ToString(), "([a-z])([A-Z])", "$1 $2");
                }

                characterData.Bests.Add(bossId, best);
                characterData.Medians.Add(bossId, median);
                characterData.Kills.Add(bossId, kills);
                characterData.Jobs.Add(bossId, job);
            }
        }
    }
}