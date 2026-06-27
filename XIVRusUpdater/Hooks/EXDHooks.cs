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

    [ThreadStatic]
    private static ExcelSheet* _lastSheet;
    [ThreadStatic]
    private static uint _lastRowId;
    [ThreadStatic]
    private static uint _lastRowIndex;
    [ThreadStatic]
    private static uint _lastResolvedRowId;
    [ThreadStatic]
    private static int _resolveCallCount;

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
            _lastSheet = sheet;
            _lastRowId = rowId;
        }
        return result;
    }

    private unsafe IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = _hGetRowByIndex!.Original(sheet, rowIndex, descriptor);
        if (sheet != null)
        {
            _lastSheet = sheet;
            _lastRowIndex = rowIndex;
        }
        return result;
    }

    private unsafe void* Detour_ResolveStringColumnIndirection(void* columnPtr)
    {
        var result = _hResolveIndirection!.Original(columnPtr);

        var sheet = _lastSheet;
        if (sheet == null) return result;

        var name = sheet->SheetName.ToString();
        if (name != "Quest") return result;

        if (_lastRowId != _lastResolvedRowId)
        {
            _lastResolvedRowId = _lastRowId;
            _resolveCallCount = 0;
        }

        _resolveCallCount++;

        if (_resolveCallCount == 1)
        {
            var str = Marshal.PtrToStringUTF8((nint)result);
            var translated = Utf8String.FromString("FunnyScout, where is 7.5 translation?");
            _translationStrings.Add((nint)translated);
            Plugin.Log.Information($"[Patch] Quest rowId={_lastRowId} resolveCall={_resolveCallCount} \"{str}\" → patched");
            return translated->StringPtr;
        }

        return result;
    }
}
