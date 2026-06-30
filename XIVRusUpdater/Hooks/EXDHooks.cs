using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private readonly Hook<GetRowByIdDelegate> _hGetRowById;
    private readonly Hook<GetRowByIndexDelegate> _hGetRowByIndex;
    private readonly Hook<ResolveStringColumnIndirectionDelegate> _hResolveIndirection;

    private delegate IExcelRowWrapper* GetRowByIndexDelegate(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor);
    private delegate IExcelRowWrapper* GetRowByIdDelegate(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null);
    private delegate void* ResolveStringColumnIndirectionDelegate(void* columnPtr);

    private readonly List<nint> _translationStrings = new();

    [ThreadStatic] private static ExcelContext context;
    public EXDHooks()
    {
        context = new ExcelContext();
        var provider = Plugin.interopProvider;

        _hGetRowById = provider.HookFromAddress<GetRowByIdDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowById,
            Detour_GetRowById
            );

        _hGetRowByIndex = provider.HookFromAddress<GetRowByIndexDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByIndex,
            Detour_GetRowByIndex
        );

        _hResolveIndirection = provider.HookFromAddress<ResolveStringColumnIndirectionDelegate>(
            ExcelRow.MemberFunctionPointers.ResolveStringColumnIndirection,
            Detour_ResolveStringColumnIndirection
        );

        _hGetRowById.Enable();
        _hGetRowByIndex.Enable();
        _hResolveIndirection.Enable();
    }

    public void Reset()
    {
        foreach (var ptr in _translationStrings)
            IMemorySpace.Free((Utf8String*)ptr);
        _translationStrings.Clear();
    }

    public void Dispose()
    {
        _hGetRowById.Disable();
        _hGetRowByIndex.Disable();
        _hResolveIndirection.Disable();

        _hGetRowById.Dispose();
        _hGetRowByIndex.Dispose();
        _hResolveIndirection.Dispose();
    }

    private unsafe IExcelRowWrapper* Detour_GetRowById(ExcelSheet* sheet, uint rowId, uint* outErrorCode)
    {
        var result = _hGetRowById!.Original(sheet, rowId, outErrorCode);
        if (sheet != null)
        {
            context.sheetName = sheet->SheetName.ToString();
            context.lastRowId = rowId;
        }
        return result;
    }

    private unsafe IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = _hGetRowByIndex!.Original(sheet, rowIndex, descriptor);
        if (sheet != null)
        {
            context.sheetName = sheet->SheetName.ToString();
            context.lastRowId = descriptor->RowId;
        }
        return result;
    }

    private unsafe void* Detour_ResolveStringColumnIndirection(void* columnPtr)
    {
        var result = _hResolveIndirection!.Original(columnPtr);

        var name = context.sheetName;
        
        if (context.lastRowId != context.lastResolvedRowId)
        {
            context.lastResolvedRowId = context.lastRowId;
            context.resolveCallCount = 0;
        }

        context.resolveCallCount++;

        /*
        if (context.resolveCallCount == 1)
        {
            var str = Marshal.PtrToStringUTF8((nint)result);
            var translated = Utf8String.FromString("FunnyScout, where is 7.5 translation?");
            _translationStrings.Add((nint)translated);
            Plugin.Log.Information($"[Patch] Quest rowId={context.lastRowId} resolveCall={context.resolveCallCount} \"{str}\" → patched");
            return translated->StringPtr;
        }
        */

        return result;
    }
}


public class ExcelContext()
{
    public string? sheetName { get; set; }
    public uint lastRowId { get; set; }
    public uint lastResolvedRowId { get; set; }
    public int resolveCallCount { get; set; }
}
