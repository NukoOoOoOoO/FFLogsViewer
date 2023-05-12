using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class HeaderBar
{
    public uint ResetSizeCount;

    private readonly Stopwatch partyListStopwatch = new();

    public void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));

        var buttonsWidth = GetButtonsWidth();
        var minWindowSize = GetMinWindowSize();

        if (ImGui.GetWindowSize().X < minWindowSize || this.ResetSizeCount != 0)
        {
            ImGui.SetWindowSize(new Vector2(minWindowSize, -1));
        }

        if (!Service.Configuration.Style.IsSizeFixed
            && (Service.Configuration.Style.MainWindowFlags & ImGuiWindowFlags.AlwaysAutoResize) == 0)
        {
            ImGui.SetWindowSize(new Vector2(Service.Configuration.Style.MinMainWindowWidth > minWindowSize ? Service.Configuration.Style.MinMainWindowWidth : minWindowSize, -1));
        }

        // I hate ImGui
        var contentRegionAvailWidth = ImGui.GetContentRegionAvail().X;
        if (ImGui.GetWindowSize().X < minWindowSize || this.ResetSizeCount != 0)
        {
            contentRegionAvailWidth = minWindowSize - (ImGui.GetStyle().WindowPadding.X * 2);
            this.ResetSizeCount--;
        }

        var calcInputSize = (contentRegionAvailWidth - (ImGui.GetStyle().ItemSpacing.X * 2) - buttonsWidth) / 3;

        ImGui.SetNextItemWidth(calcInputSize);
        if (ImGui.InputTextWithHint("##FirstName", Service.Localization.GetString("Main_Name"), ref Service.CharDataManager.DisplayedChar.FirstName, 18, ImGuiInputTextFlags.CharsNoBlank))
        {
            Service.CharDataManager.DisplayedChar.FirstName = Service.CharDataManager.DisplayedChar.FirstName[..Math.Min(Service.CharDataManager.DisplayedChar.FirstName.Length, 6)];
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(calcInputSize);
        ImGui.InputTextWithHint("##WorldName", Service.Localization.GetString("Main_World"), ref Service.CharDataManager.DisplayedChar.WorldName, 18, ImGuiInputTextFlags.CharsNoBlank);

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Search))
        {
            Service.CharDataManager.DisplayedChar.FetchLogs();
        }

        Util.SetHoverTooltip(Service.Localization.GetString("Main_Search"));

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Crosshairs))
        {
            Service.CharDataManager.DisplayedChar.FetchTargetChar();
        }

        Util.SetHoverTooltip(Service.Localization.GetString("Main_Target"));

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Clipboard))
        {
            Service.CharDataManager.DisplayedChar.FetchClipboardCharacter();
        }

        Util.SetHoverTooltip(Service.Localization.GetString("Main_SearchClipboard"));

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.UsersCog))
        {
            ImGui.OpenPopup("##TeamList");
        }

        Util.SetHoverTooltip(Service.Localization.GetString("Main_PartyMembers"));

        if (ImGui.BeginPopup("##TeamList", ImGuiWindowFlags.NoMove))
        {
            Util.UpdateDelayed(this.partyListStopwatch, TimeSpan.FromSeconds(1), Service.TeamManager.UpdateTeamList);

            var partyList = Service.TeamManager.TeamList;
            if (partyList.Count != 0)
            {
                if (ImGui.BeginTable("##PartyListTable", 3, ImGuiTableFlags.RowBg))
                {
                    for (var i = 0; i < partyList.Count; i++)
                    {
                        if (i != 0)
                        {
                            ImGui.TableNextRow();
                        }

                        ImGui.TableNextColumn();

                        var partyMember = partyList[i];
                        var iconSize = 25 * ImGuiHelpers.GlobalScale;
                        var middleCursorPosY = ImGui.GetCursorPosY() + (iconSize / 2) - (ImGui.GetFontSize() / 2);

                        if (ImGui.Selectable($"##PartyListSel{i}", false, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, iconSize)))
                        {
                            Service.CharDataManager.DisplayedChar.FetchCharacter($"{partyMember.FirstName}@{partyMember.World}");
                        }

                        var icon = Service.GameDataManager.JobIconsManager.GetJobIcon(partyMember.JobId);
                        if (icon != null)
                        {
                            ImGui.SameLine();
                            ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize));
                        }
                        else
                        {
                            ImGui.SetCursorPosY(middleCursorPosY);
                            ImGui.Text("(?)");
                        }

                        ImGui.TableNextColumn();

                        ImGui.SetCursorPosY(middleCursorPosY);
                        ImGui.Text($"{partyMember.FirstName}");

                        ImGui.TableNextColumn();

                        ImGui.SetCursorPosY(middleCursorPosY);
                        ImGui.Text(partyMember.World + " ");
                    }

                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Text(Service.Localization.GetString("Main_NoPartyMember"));
            }

            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);

        if (!Service.FFLogsClient.IsTokenValid)
        {
            var message = FFLogsClient.IsConfigSet()
                              ? Service.Localization.GetString("Main_InvalidAPIClient")
                              : Service.Localization.GetString("Main_APIClientNotSetup");
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            if (Util.CenterSelectable(message))
            {
                Service.ConfigWindow.IsOpen = true;
            }

            ImGui.PopStyleColor();

            return;
        }

        if (Service.CharDataManager.DisplayedChar.CharError == null)
        {
            if (Service.CharDataManager.DisplayedChar.IsDataLoading)
            {
                Util.CenterText(Service.Localization.GetString("Main_Loading"));
            }
            else
            {
                if (Service.CharDataManager.DisplayedChar.IsDataReady)
                {
                    if (Util.CenterSelectable(
                            Service.Localization.GetString("Main_ViewingLogs").Replace("{Name}", Service.CharDataManager.DisplayedChar.LoadedFirstName).Replace("{World}", Service.CharDataManager.DisplayedChar.LoadedWorldName)))
                    {
                        Util.OpenLink(Service.CharDataManager.DisplayedChar);
                    }

                    Util.SetHoverTooltip(Service.Localization.GetString("Main_OpenOnFFLogs"));
                }
                else
                {
                    Util.CenterText(Service.Localization.GetString("Main_Waiting"));
                }
            }
        }
        else
        {
            Util.CenterText(Util.GetErrorMessage(Service.CharDataManager.DisplayedChar), ImGuiColors.DalamudRed);
        }

        if (Service.Configuration.Layout.Count == 0)
        {
            if (Util.CenterSelectable(Service.Localization.GetString("Main_NoLayoutSetup")))
            {
                Service.ConfigWindow.IsOpen = true;
            }
        }
    }

    private static float GetButtonsWidth()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var buttonsWidth =
            ImGui.CalcTextSize(FontAwesomeIcon.Search.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.Crosshairs.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.Clipboard.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.UsersCog.ToIconString()).X +
            (ImGui.GetStyle().ItemSpacing.X * 4) + // between items
            (ImGui.GetStyle().FramePadding.X * 8); // around buttons, 2 per
        ImGui.PopFont();
        return buttonsWidth;
    }

    private static float GetMinInputWidth()
    {
        return new[]
        {
            ImGui.CalcTextSize(Service.Localization.GetString("Main_Name")).X,
            ImGui.CalcTextSize(Service.Localization.GetString("Main_World")).X,
            ImGui.CalcTextSize(Service.CharDataManager.DisplayedChar.FirstName).X,
            ImGui.CalcTextSize(Service.CharDataManager.DisplayedChar.WorldName).X,
        }.Max() + (ImGui.GetStyle().FramePadding.X * 2);
    }

    private static float GetMinWindowSize()
    {
        return ((GetMinInputWidth() + (ImGui.GetStyle().ItemSpacing.X * 2)) * 3) + GetButtonsWidth();
    }
}
