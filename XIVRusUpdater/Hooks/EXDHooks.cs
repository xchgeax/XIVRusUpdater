using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using XIVRusUpdater.Core;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private readonly Hook<GetRowByIdDelegate> hGetRowById = null!;
    private readonly Hook<GetRowByIndexDelegate> hGetRowByIndex = null!;
    private readonly Hook<GetRowByDescriptorDelegate> hGetRowByDescriptor = null!;
    private readonly Hook<GetSubRowByDescriptorDelegate> hGetSubRowByDescriptor = null!;
    private readonly Hook<ResolveStringColumnIndirectionDelegate> hResolveIndirection = null!;

    private delegate IExcelRowWrapper* GetRowByIndexDelegate(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor);
    private delegate IExcelRowWrapper* GetRowByIdDelegate(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null);
    private delegate IExcelRowWrapper* GetRowByDescriptorDelegate(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode);
    private delegate IExcelRowWrapper* GetSubRowByDescriptorDelegate(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode);
    private delegate void* ResolveStringColumnIndirectionDelegate(void* columnPtr);

    private readonly TranslationParser parser;
    private readonly LruCache<nint, ColumnInfo> columnMap = new(capacity: 8192);
    private readonly ReaderWriterLockSlim cacheLock = new();

    public EXDHooks(IGameInteropProvider provider)
    {
        parser = new TranslationParser();

        hGetRowById = provider.HookFromAddress<GetRowByIdDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowById, Detour_GetRowById);

        hGetRowByIndex = provider.HookFromAddress<GetRowByIndexDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByIndex, Detour_GetRowByIndex);

        hGetRowByDescriptor = provider.HookFromAddress<GetRowByDescriptorDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByDescriptor, Detour_GetRowByDescriptor);

        hGetSubRowByDescriptor = provider.HookFromAddress<GetSubRowByDescriptorDelegate>(
            ExcelSheet.MemberFunctionPointers.GetSubRowByDescriptor, Detour_GetSubRowByDescriptor);

        hResolveIndirection = provider.HookFromAddress<ResolveStringColumnIndirectionDelegate>(
            ExcelRow.MemberFunctionPointers.ResolveStringColumnIndirection, Detour_ResolveStringColumnIndirection);

        EnableAll();
    }

    public void EnableAll()
    {
        hGetRowById.Enable();
        hGetRowByIndex.Enable();
        hGetRowByDescriptor.Enable();
        hGetSubRowByDescriptor.Enable();
        hResolveIndirection.Enable();
    }

    public void DisableAll()
    {
        hGetRowById.Disable();
        hGetRowByIndex.Disable();
        hGetRowByDescriptor.Disable();
        hGetSubRowByDescriptor.Disable();
        hResolveIndirection.Disable();
    }

    public void Dispose()
    {
        DisableAll();

        hGetRowById.Dispose();
        hGetRowByIndex.Dispose();
        hGetRowByDescriptor.Dispose();
        hGetSubRowByDescriptor.Dispose();
        hResolveIndirection.Dispose();

        parser.Dispose();
        cacheLock.Dispose();
    }

    private IExcelRowWrapper* Detour_GetRowById(ExcelSheet* sheet, uint rowId, uint* outErrorCode)
    {
        var result = hGetRowById.Original(sheet, rowId, outErrorCode);
        PopulateRowMap(result, sheet, rowId: rowId, descriptor: null, source: nameof(Detour_GetRowById));
        return result;
    }

    private IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = hGetRowByIndex.Original(sheet, rowIndex, descriptor);
        PopulateRowMap(result, sheet, rowId: null, descriptor: descriptor, source: nameof(Detour_GetRowByIndex));
        return result;
    }

    private IExcelRowWrapper* Detour_GetRowByDescriptor(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode)
    {
        var result = hGetRowByDescriptor.Original(sheet, descriptor, outErrorCode);
        // TODO: вероятнее всего не вызывается в нужных нам местах
        //PopulateRowMap(result, sheet, rowId: null, descriptor: descriptor, source: nameof(Detour_GetRowByDescriptor));
        return result;
    }

    private IExcelRowWrapper* Detour_GetSubRowByDescriptor(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode)
    {
        var result = hGetSubRowByDescriptor.Original(sheet, descriptor, outErrorCode);
        PopulateRowMap(result, sheet, rowId: null, descriptor: descriptor, source: nameof(Detour_GetSubRowByDescriptor));
        return result;
    }

    private void* Detour_ResolveStringColumnIndirection(void* columnPtr)
    {
        var result = hResolveIndirection.Original(columnPtr);
        nint ptr = (nint)columnPtr;

        ColumnInfo info;
        cacheLock.EnterReadLock();
        try
        {
            if (!columnMap.TryGetValue(ptr, out info))
                return result;
        }
        finally
        {
            cacheLock.ExitReadLock();
        }

        if (Plugin.filter.IsActive(info.SheetName) &&
            parser.TryGetValue(info.SheetName, info.RowId, info.ColumnIndex, out var translation))
        {
            return translation!.Pointer;
        }

        return result;
    }

    private void PopulateRowMap(
        IExcelRowWrapper* wrapper,
        ExcelSheet* sheet,
        uint? rowId,
        ExcelRowDescriptor* descriptor,
        string source)
    {
        if (wrapper == null || wrapper->Row == null)
            return;

        ExcelRow* row = wrapper->Row;
        ExcelSheet* activeSheet = row->Sheet != null ? row->Sheet : sheet;

        if (activeSheet == null)
            return;

        string sheetName = activeSheet->SheetName.ToString();
        if (string.IsNullOrEmpty(sheetName))
            return;

        uint resolvedRowId = rowId ?? (descriptor != null ? descriptor->RowId : 0);

        uint columnCount = activeSheet->ColumnCount;
        ref readonly var sheetRef = ref *activeSheet;

        cacheLock.EnterWriteLock();
        try
        {
            for (uint columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                int stringColumnIndex = sheetRef.ToStringColumnIndex(columnIndex);
                if (stringColumnIndex < 0)
                    continue;

                void* columnPtr = row->GetColumnPtr(columnIndex);
                if (columnPtr == null)
                    continue;

                columnMap.Add((nint)columnPtr, new ColumnInfo(
                    Supplier: source,
                    RowId: resolvedRowId,
                    ColumnIndex: (uint)stringColumnIndex,
                    SheetIndex: activeSheet->SheetIndex,
                    SheetName: sheetName
                ));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"[{source}] Failed to populate column cache for sheet '{sheetName}'.");
        }
        finally
        {
            cacheLock.ExitWriteLock();
        }
    }
}

public readonly record struct ColumnInfo(
    string Supplier,
    uint RowId,
    uint ColumnIndex,
    uint SheetIndex,
    string SheetName
);
