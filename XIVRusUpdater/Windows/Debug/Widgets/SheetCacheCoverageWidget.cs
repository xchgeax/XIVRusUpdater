using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using XIVRusUpdater.Hooks;

namespace XIVRusUpdater.Windows.Debug.Widgets;

public sealed class SheetCacheCoverageWidget : IDebugWindowWidget
{
    public string[]? CommandShortcuts { get; init; } = ["cache", "coverage"];
    public string DisplayName { get; init; } = "Sheet Cache";
    public bool Ready { get; set; }

    private CacheSnapshotEntry[] snapshot = [];
    private DateTime lastLoadedAt;
    private string? lastError;

    public void Load()
    {
        try
        {
            snapshot = Plugin.HookLayers.GetCacheSnapshot();
            lastError = null;
        }
        catch (Exception ex)
        {
            snapshot = [];
            lastError = ex.Message;
        }
        finally
        {
            lastLoadedAt = DateTime.Now;
            Ready = true;
        }
    }

    public void Draw()
    {
        if (!Ready)
        {
            ImGui.TextDisabled("columnMap not readed.");
            if (ImGui.Button("Load##cache_coverage"))
                Load();
            return;
        }

        if (lastError is not null)
        {
            ImGui.TextColored(new Vector4(0.90f, 0.30f, 0.30f, 1f), $"Cache Read Error: {lastError}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Retry##cache_coverage"))
                Load();
            return;
        }

        var totalRows = snapshot.Sum(s => s.RowCount);
        ImGui.TextDisabled($"Snapshot {lastLoadedAt:T} — {snapshot.Length} sheets, {totalRows} rows cached");
        ImGui.SameLine();
        if (ImGui.SmallButton("Refresh##cache_coverage"))
            Load();

        if (!ImGui.BeginTable("##sheet_cache_coverage_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable))
            return;

        ImGui.TableSetupColumn("Sheet");
        ImGui.TableSetupColumn("Cached rows");
        ImGui.TableSetupColumn("Unique columns");
        ImGui.TableHeadersRow();

        foreach (var entry in snapshot)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.SheetName);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.RowCount.ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(entry.ColumnCount.ToString());
        }

        ImGui.EndTable();
    }
}
