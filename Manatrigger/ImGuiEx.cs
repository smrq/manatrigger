using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Lumina.Excel;

namespace ImGuiNET
{
    public static partial class ImGuiEx
    {
        public static bool ConfirmationButton(string text, string confirmationText, Vector2 size = default)
        {
            ImGui.Button(text, size);

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && ImGui.IsItemHovered())
            {
                return true;
            }

            if (!ImGui.BeginPopupContextItem(null, ImGuiPopupFlags.MouseButtonLeft))
            {
                return false;
            }

            var result = ImGui.Selectable(confirmationText);
            ImGui.EndPopup();
            return result;
        }
    }
}
