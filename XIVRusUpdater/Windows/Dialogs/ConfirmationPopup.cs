using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using XIVRusUpdater.Utils;

namespace XIVRusUpdater.Windows.Dialogs;

public sealed class ConfirmationPopup
{
    private readonly string popupId;

    public bool IsOpen { get; private set; }

    public Action? OnConfirm;
    public Action? OnCancel;

    public ConfirmationPopup(string popupId)
    {
        this.popupId = popupId;
    }

    public void Open()
    {
        IsOpen = true;
        ImGui.OpenPopup(popupId);
    }

    public void Draw()
    {
        if (!ImGui.BeginPopupModal(popupId, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text(Translations.RestartWarning);

        ImGui.TextWrapped(Translations.ConfirmationQuestion);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(Translations.RestartQuestion);

        ImGui.Spacing();

        if (ImGui.Button(Translations.ConfirmationConfirm))
        {
            OnConfirm?.Invoke();
            ImGui.CloseCurrentPopup();
            IsOpen = false;
        }

        ImGui.SameLine();

        if (ImGui.Button(Translations.ConfirmationCancel))
        {
            OnCancel?.Invoke();
            ImGui.CloseCurrentPopup();
            IsOpen = false;
        }

        ImGui.EndPopup();
    }
}
