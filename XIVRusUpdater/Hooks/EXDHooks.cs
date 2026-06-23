using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private readonly Hook<GetRowByIdDelegate> _hGetRowById;
    private readonly Hook<GetRowByIndexDelegate> _hGetRowByIndex;

    private delegate IExcelRowWrapper* GetRowByIndexDelegate(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor);
    private delegate IExcelRowWrapper* GetRowByIdDelegate(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null);

    public EXDHooks()
    {
        var provider = Plugin.interopProvider;

        _hGetRowById = provider.HookFromAddress<GetRowByIdDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowById,
            Detour_GetRowById
            );

        _hGetRowByIndex = provider.HookFromAddress<GetRowByIndexDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByIndex,
            Detour_GetRowByIndex
        );

        _hGetRowById.Enable();
        _hGetRowByIndex.Enable();
    }

    public void Dispose()
    {
        _hGetRowById.Disable();
        _hGetRowByIndex.Disable();

        _hGetRowById.Dispose();
        _hGetRowByIndex.Dispose();
    }

    private static void Log(string msg)
        => Plugin.Log.Information(msg);

    private readonly HashSet<(string sheet, uint row)> _seenById = new();
    private readonly HashSet<(string sheet, uint row)> _seenByIndex = new();

    public string GetSeeningSheets(string filter)
    {
        var list = _seenById.Select(x => x.sheet);
        list.Union(_seenByIndex.Select(x => x.sheet));
        return string.Join(",", list.Where(elem => elem.Contains(filter, StringComparison.OrdinalIgnoreCase)).Distinct() );
    }

    private unsafe IExcelRowWrapper* Detour_GetRowById(
    ExcelSheet* sheet, uint rowId, uint* outErrorCode)
    {
        var result = _hGetRowById!.Original(sheet, rowId, outErrorCode);

        var key = (sheet->SheetName.ToString(), rowId);
        
        if (!_seenById.Add(key)) return result;

        var span = sheet->ColumnDefinitionSpan;
        var sb = new StringBuilder();
        for (int i = 0; i < span.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var col = span[i];
            sb.Append(Enum.GetName((ExcelColumnType)col.Type) ?? col.Type.ToString());
            sb.Append('|').Append(col.Index);
            sb.Append('|').Append(col.Offset);
        }

        if (key.Item1.Contains("quest", StringComparison.OrdinalIgnoreCase))
            Log($"[GetRowById] RowId={rowId} Sheet={sheet->SheetName} Columns={sb}");
        return result;
    }

    private unsafe IExcelRowWrapper* Detour_GetRowByIndex(
        ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = _hGetRowByIndex!.Original(sheet, rowIndex, descriptor);
        
        if (sheet == null) return result;

        var key = (sheet->SheetName.ToString(), rowIndex);
        if (!_seenByIndex.Add(key)) return result;

        var span = sheet->ColumnDefinitionSpan;
        var sb = new StringBuilder();
        for (int i = 0; i < span.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var col = span[i];
            sb.Append(Enum.GetName((ExcelColumnType)col.Type) ?? col.Type.ToString());
            sb.Append('|').Append(col.Index);
            sb.Append('|').Append(col.Offset);
        }

        if (key.Item1.Contains("quest", StringComparison.OrdinalIgnoreCase))
            Log($"[GetRowByIndex] RowIndex={rowIndex} Sheet={sheet->SheetName} Columns={sb}");
        return result;
    }
}
