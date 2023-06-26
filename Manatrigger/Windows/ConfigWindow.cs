using System;
using System.Data;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace Manatrigger.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly DataManager dataManager;

    private int selectedTrigger = -1;
    private string actionSearchText = string.Empty;

    public ConfigWindow(Plugin plugin) : base(
        "Manatrigger",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(525, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        configuration = plugin.Configuration;
        dataManager = plugin.DataManager;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.BeginTable("table", 2);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawTriggerList();
        ImGui.TableNextColumn();
        DrawTriggerEdit();
        ImGui.EndTable();
    }

    public void DrawTriggerList()
    {
        ImGui.PushID("TriggerList");

        if (ImGui.Button("Add trigger"))
        {
            OnAddTrigger();
        }

        ImGui.SameLine();

        if (ImGuiEx.ConfirmationButton("Delete trigger", "Confirm delete?\n(Right click to skip confirmation)"))
        {
            OnDeleteTrigger(selectedTrigger);
        }

        ImGui.Spacing();

        ImGui.BeginChild("trigger-list", new Vector2(-1, -1), true);
        for (var i = 0; i < configuration.Triggers.Count; i++)
        {
            var trigger = configuration.Triggers[i];
            ImGui.PushID(i);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(trigger.Enabled ? ImGuiCol.Text : ImGuiCol.TextDisabled));
            if (ImGui.Selectable(trigger.Name, selectedTrigger == i))
            {
                OnSelectTrigger(i);
            }
            ImGui.PopStyleColor();
            ImGui.PopID();
        }
        ImGui.EndChild();
        ImGui.PopID();
    }

    public void DrawTriggerEdit()
    {
        if (selectedTrigger == -1)
        {
            return;
        }
        var trigger = configuration.Triggers[selectedTrigger];

        ImGui.PushID("TriggerEdit");

        var name = trigger.Name;
        if (ImGui.InputText("Name", ref name, 100))
        {
            OnTriggerUpdateName(trigger, name);
        }

        ImGui.SameLine();

        var enabled = trigger.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            OnTriggerUpdateEnabled(trigger, enabled);
        }

        ImGui.Spacing();

        ImGui.Text("Actions");
        var actionSheet = dataManager.GetExcelSheet<Action>()!;
        for (var i = 0; i < trigger.Actions.Count; i++)
        {
            var action = trigger.Actions[i];
            var actionRow = actionSheet.GetRow(action.Id)!;

            ImGui.PushID(i);
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Times.ToIconString()))
            {
                OnTriggerRemoveAction(trigger, i);
            }
            ImGui.PopFont();
            ImGui.SameLine();

            ImGui.Text($"[#{actionRow.RowId}] {actionRow.Name}");
            ImGui.PopID();
        }

        ImGui.Button("Add action");
        if (ImGui.BeginPopupContextItem(null, ImGuiPopupFlags.MouseButtonLeft))
        {
            ImGui.PushID("add-action-popup");
            ImGui.InputTextWithHint("##search", "Search", ref actionSearchText, 128, ImGuiInputTextFlags.AutoSelectAll);
            var searchTrimmed = actionSearchText.Trim();

            ImGui.BeginChild("search-list", new Vector2(0, 200 * ImGuiHelpers.GlobalScale), true);
            foreach (var row in actionSheet)
            {
                var formattedRow = $"[#{row.RowId}] {row.Name}";
                if (searchTrimmed != string.Empty && !formattedRow.Contains(searchTrimmed, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                if (!row.IsPlayerAction)
                {
                    continue;
                }

                if (ImGui.Selectable(formattedRow, false))
                {
                    OnTriggerAddAction(trigger, row.RowId);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndChild();

            ImGui.PopID();
            ImGui.EndPopup();
        }

        ImGui.Spacing();

        ImGui.Text("Macro");
        ImGui.PushItemWidth(-1);
        var macro = trigger.Macro;
        if (ImGui.InputText($"##{trigger.Name}-macro", ref macro, 100_000))
        {
            OnTriggerUpdateMacro(trigger, macro);
        }
        ImGui.PopItemWidth();

        ImGui.PopID();
    }

    private void OnAddTrigger()
    {
        configuration.Triggers.Add(new() { Name = "Untitled Trigger" });
        selectedTrigger = configuration.Triggers.Count - 1;
        configuration.Save();
    }

    private void OnDeleteTrigger(int index)
    {
        configuration.Triggers.RemoveAt(index);
        selectedTrigger = Math.Min(selectedTrigger, configuration.Triggers.Count - 1);
        configuration.Save();
    }

    private void OnSelectTrigger(int index)
    {
        selectedTrigger = index;
    }

    private void OnTriggerUpdateName(Configuration.Trigger trigger, string name)
    {
        trigger.Name = name;
        configuration.Save();
    }

    private void OnTriggerUpdateEnabled(Configuration.Trigger trigger, bool enabled)
    {
        trigger.Enabled = enabled;
        configuration.Save();
    }

    private void OnTriggerAddAction(Configuration.Trigger trigger, uint actionId)
    {
        trigger.Actions.Add(new()
        {
            Id = actionId
        });
        trigger.Actions = trigger.Actions.OrderBy(item => item.Id).ToList();
        configuration.Save();
    }

    private void OnTriggerRemoveAction(Configuration.Trigger trigger, int index)
    {
        trigger.Actions.RemoveAt(index);
        configuration.Save();
    }

    private void OnTriggerUpdateMacro(Configuration.Trigger trigger, string macro)
    {
        trigger.Macro = macro;
        configuration.Save();
    }
}
