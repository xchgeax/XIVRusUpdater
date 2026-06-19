using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Exd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FFXIVClientStructs.FFXIV.Component.Completion.CompletionModule;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private readonly Hook<GetRowBySheetAndRowIdDelegate> _h1;
    private readonly Hook<GetRowBySheetAndRowIndexDelegate> _h2;
    private readonly Hook<GetRowBySheetIndexAndRowIdDelegate> _h3;
    private readonly Hook<GetRowBySheetIndexAndRowIndexDelegate> _h4;

    private List<string> ignoreFiles = ["MapType", "TextCommand", "BGM", "TerritoryType", "TerritoryTypeTransient", "TomestonesItem", "Action", "ClassJob", "ClassJobCategory", "Battalion", "Permission", "ActionCostType", "Trait", "GeneralAction"];

    private delegate ExcelRow* GetRowBySheetAndRowIdDelegate(
        ExdModule* exd, ExcelSheet* sheet, uint rowId);

    private delegate ExcelRow* GetRowBySheetAndRowIndexDelegate(
        ExdModule* exd, ExcelSheet* sheet, uint rowIndex);

    private delegate ExcelRow* GetRowBySheetIndexAndRowIdDelegate(
        ExdModule* exd, uint sheetIndex, uint rowId);

    private delegate ExcelRow* GetRowBySheetIndexAndRowIndexDelegate(
        ExdModule* exd, uint sheetIndex, uint rowIndex);

    public EXDHooks()
    {
        var provider = Plugin.interopProvider;

        _h1 = provider.HookFromAddress<GetRowBySheetAndRowIdDelegate>(
            ExdModule.MemberFunctionPointers.GetRowBySheetAndRowId,
            Detour_GetRowBySheetAndRowId);

        _h2 = provider.HookFromAddress<GetRowBySheetAndRowIndexDelegate>(
            ExdModule.MemberFunctionPointers.GetRowBySheetAndRowIndex,
            Detour_GetRowBySheetAndRowIndex);

        _h3 = provider.HookFromAddress<GetRowBySheetIndexAndRowIdDelegate>(
            ExdModule.MemberFunctionPointers.GetRowBySheetIndexAndRowId,
            Detour_GetRowBySheetIndexAndRowId);

        _h4 = provider.HookFromAddress<GetRowBySheetIndexAndRowIndexDelegate>(
            ExdModule.MemberFunctionPointers.GetRowBySheetIndexAndRowIndex,
            Detour_GetRowBySheetIndexAndRowIndex);

        _h1.Enable();
        _h2.Enable();
        _h3.Enable();
        _h4.Enable();
    }

    public void Dispose()
    {
        _h1.Dispose();
        _h2.Dispose();
        _h3.Dispose();
        _h4.Dispose();
    }

    private static string SheetName(ExcelSheet* sheet)
        => sheet == null ? "null" : sheet->SheetName.ToString();

    private static void Log(string msg)
        => Plugin.Log.Information(msg);

    private unsafe ExcelRow* Detour_GetRowBySheetAndRowId(
        ExdModule* exd, ExcelSheet* sheet, uint rowId)
    {
        var result = _h1.Original(exd, sheet, rowId);

        var sheetName = SheetName(sheet);

        if (!ignoreFiles.Contains(sheetName)) Log($"[EXD] Sheet={sheetName}({sheet->SheetIndex}) RowId={rowId} Result=0x{(nint)result:X}");

        return result;
    }

    private unsafe ExcelRow* Detour_GetRowBySheetAndRowIndex(
        ExdModule* exd, ExcelSheet* sheet, uint rowIndex)
    {
        var result = _h2.Original(exd, sheet, rowIndex);

        var sheetName = SheetName(sheet);

        // if (!ignoreFiles.Contains(sheetName))  Log($"[EXD] Sheet={sheetName}({sheet->SheetIndex}) RowIndex={rowIndex} Result=0x{(nint)result:X}");

        return result;
    }

    private unsafe ExcelRow* Detour_GetRowBySheetIndexAndRowId(
        ExdModule* exd, uint sheetIndex, uint rowId)
    {
        var result = _h3.Original(exd, sheetIndex, rowId);

        var sheetName = exd->GetSheetByIndex(sheetIndex)->SheetName;

        // if (!ignoreFiles.Contains(sheetName.ToString()))  Log($"[EXD] SheetIndex={sheetIndex} ({sheetName}) RowId={rowId} Result=0x{(nint)result:X}");

        return result;
    }

    private unsafe ExcelRow* Detour_GetRowBySheetIndexAndRowIndex(
        ExdModule* exd, uint sheetIndex, uint rowIndex)
    {
        var result = _h4.Original(exd, sheetIndex, rowIndex);

        var sheetName = exd->GetSheetByIndex(sheetIndex)->SheetName;

        // if (!ignoreFiles.Contains(sheetName.ToString())) Log($"[EXD] SheetIndex={sheetIndex} ({sheetName}) RowIndex={rowIndex} Result=0x{(nint)result:X}");

        return result;
    }
}
