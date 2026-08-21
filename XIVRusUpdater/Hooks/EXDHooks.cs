using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using XIVRusUpdater.Core;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private readonly Hook<GetRowByIdDelegate> _hGetRowById;
    private readonly Hook<GetRowByIndexDelegate> _hGetRowByIndex;
    private readonly Hook<ResolveStringColumnIndirectionDelegate> _hResolveIndirection;

    private delegate IExcelRowWrapper* GetRowByIndexDelegate(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor);
    private delegate IExcelRowWrapper* GetRowByIdDelegate(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null);
    private delegate void* ResolveStringColumnIndirectionDelegate(void* columnPtr);

    private TranslationParser _parser;

    [ThreadStatic] private static ExcelContext? context;
    private static ExcelContext Context => context ??= new ExcelContext();

    public EXDHooks()
    {
        context = new ExcelContext();
        _parser = new TranslationParser();
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

    public void EnableAll()
    {
        _hGetRowById.Enable();
        _hGetRowByIndex.Enable();
        _hResolveIndirection.Enable();
    }

    public void DisableAll()
    {
        _hGetRowById.Disable();
        _hGetRowByIndex.Disable();
        _hResolveIndirection.Disable();
    }

    public void Dispose()
    {
        DisableAll();

        _hGetRowById.Dispose();
        _hGetRowByIndex.Dispose();
        _hResolveIndirection.Dispose();

        _parser.Dispose();
    }

    private unsafe IExcelRowWrapper* Detour_GetRowById(ExcelSheet* sheet, uint rowId, uint* outErrorCode)
    {
        var result = _hGetRowById!.Original(sheet, rowId, outErrorCode);
        if (sheet != null)
        {
            var ctx = Context;
            ctx.sheetName = sheet->SheetName.ToString();
            ctx.lastRowId = rowId;
        }
        return result;
    }

    private unsafe IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = _hGetRowByIndex!.Original(sheet, rowIndex, descriptor);
        if (sheet != null)
        {
            var ctx = Context;

            ctx.sheetName = sheet->SheetName.ToString();
            ctx.lastRowId = descriptor->RowId;
        }
        return result;
    }

    private unsafe void* Detour_ResolveStringColumnIndirection(void* columnPtr)
    {
        var result = _hResolveIndirection!.Original(columnPtr);

        var ctx = Context;

        if (string.IsNullOrEmpty(ctx.sheetName))
            return result;

        if (ctx.lastRowId != ctx.lastResolvedRowId)
        {
            ctx.lastResolvedRowId = ctx.lastRowId;
            ctx.resolveCallCount = 0;
        }

        ctx.resolveCallCount++;

        if(Plugin.filter.IsActive(ctx.sheetName) && 
            _parser.TryGetValue(ctx.sheetName, ctx.lastResolvedRowId, ctx.resolveCallCount, out var translation) )
        {
            return translation!.Pointer;
        }

        return result;
    }
}


public class ExcelContext()
{
    public string? sheetName { get; set; }
    public uint lastRowId { get; set; }
    public uint lastResolvedRowId { get; set; }
    public uint resolveCallCount { get; set; }
}
