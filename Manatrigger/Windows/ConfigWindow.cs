using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Manatrigger.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    private int selectedTrigger = -1;
    private int triggerActionId = 0;

    public ConfigWindow(Plugin plugin) : base(
        "Manatrigger",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(525, 600);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Columns(2);

        DrawTriggerList();

        ImGui.NextColumn();

        DrawTriggerEdit();
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

        for (var i = 0; i < configuration.Triggers.Count; i++)
        {
            var trigger = configuration.Triggers[i];
            if (ImGui.Selectable($"{trigger.Name}##{i}", selectedTrigger == i))
            {
                OnSelectTrigger(i);
            }
        }
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

        ImGui.Spacing();
        ImGui.Text("Actions");
        for (var i = 0; i < trigger.Actions.Count; i++)
        {
            var action = trigger.Actions[i];
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Times.ToIconString()))
            {
                OnTriggerRemoveAction(trigger, i);
            }
            ImGui.PopFont();
            ImGui.SameLine();

            ImGui.Text($"#{action.Id}");
        }

        if (ImGui.InputInt("Add action", ref triggerActionId, 0, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            OnTriggerAddAction(trigger, (uint)triggerActionId);
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

    private void OnTriggerAddAction(Configuration.Trigger trigger, uint actionId)
    {
        trigger.Actions.Add(new()
        {
            Id = actionId
        });
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
