using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Exd;
using InteropGenerator.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureTextModule;
using static FFXIVClientStructs.FFXIV.Component.Completion.CompletionModule;
using static FFXIVClientStructs.FFXIV.Component.Excel.ExcelRow.Delegates;

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

    private static string SheetName(ExcelSheet* sheet)
        => sheet == null ? "null" : sheet->SheetName.ToString();

    private static void Log(string msg)
        => Plugin.Log.Information(msg);

    private readonly HashSet<(string sheet, uint row)> _seenRows = new();

    private unsafe IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = _hGetRowByIndex!.Original(sheet, rowIndex, descriptor);

        var key = (sheet->SheetName.ToString(), rowIndex);

        var span = sheet->ColumnDefinitionSpan;
        List<string> types = new List<string>(); 
        for(int i = 0; i < span.Length; ++i)
        {
            var columnType = Enum.GetName((ExcelColumnType)span[i].Type);
            if(columnType != null)
                types.Add(columnType);
        }

        if (_seenRows.Add(key))
            Log($"[GetRowByIndex] RowIndex = {rowIndex}, Sheet = {sheet->SheetName}, Columns = {string.Join(",", types)}");

        return result;
    }

    private unsafe IExcelRowWrapper* Detour_GetRowById(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null)
    {
        var result = _hGetRowById!.Original(sheet, rowId);

        var key = (sheet->SheetName.ToString(), rowId);
        var span = sheet->ColumnDefinitionSpan;
        List<string> types = new List<string>();
        for (int i = 0; i < span.Length; ++i)
        {
            var columnType = Enum.GetName((ExcelColumnType)span[i].Type);
            if (columnType != null)
                types.Add(columnType);
        }

        if (_seenRows.Add(key))
            Log($"[GetRowById] RowId = {rowId}, Sheet = {sheet->SheetName}, Columns = {string.Join(",", types)}");

        return result;
    }
}
