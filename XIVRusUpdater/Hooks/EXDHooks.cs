using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Excel;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using XIVRusUpdater.Core;
using XIVRusUpdater.Utils;
using XIVRusUpdater.Utils.Extentions;

namespace XIVRusUpdater.Hooks;

public unsafe class EXDHooks : IDisposable
{
    private const string AddonSheetName = "Addon";

    private Hook<GetRowByIdDelegate> getRowByIdHook = null!;
    private Hook<GetRowByIndexDelegate> getRowByIndexHook = null!;
    private Hook<GetRowByDescriptorDelegate> getRowByDescriptorHook = null!;
    private Hook<GetSubRowByDescriptorDelegate> getSubRowByDescriptorHook = null!;
    private Hook<ResolveStringColumnIndirectionDelegate> resolveIndirectionHook = null!;
    private Hook<FormatAddonTextApplyDelegate> formatAddonTextApplyHook = null!;

    private delegate IExcelRowWrapper* GetRowByIndexDelegate(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor);
    private delegate IExcelRowWrapper* GetRowByIdDelegate(ExcelSheet* sheet, uint rowId, uint* outErrorCode = null);
    private delegate IExcelRowWrapper* GetRowByDescriptorDelegate(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode);
    private delegate IExcelRowWrapper* GetSubRowByDescriptorDelegate(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode);
    private delegate void* ResolveStringColumnIndirectionDelegate(void* columnPtr);
    private delegate byte* FormatAddonTextApplyDelegate(
        RaptureTextModule* module,
        uint addonId,
        uint mode,
        void* localParameters,
        void* formatBuffer,
        void* normalizationBuffer);

    [ThreadStatic]
    private static uint? currentAddonId;

    private readonly TranslationParser translationParser;
    public TranslationParser parser => translationParser;
    private readonly LruCache<nint, ColumnInfo> columnMap = new(capacity: 65536);
    private readonly ConcurrentDictionary<nint, uint[]> stringColumnIndicesMap = new();

    public EXDHooks(IGameInteropProvider provider, String engineId)
    {
        translationParser = new TranslationParser(engineId);
        InitializeHooks(provider);
        EnableAll();
    }

    private void InitializeHooks(IGameInteropProvider provider)
    {
        getRowByIdHook = provider.HookFromAddress<GetRowByIdDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowById, Detour_GetRowById);

        getRowByIndexHook = provider.HookFromAddress<GetRowByIndexDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByIndex, Detour_GetRowByIndex);

        getRowByDescriptorHook = provider.HookFromAddress<GetRowByDescriptorDelegate>(
            ExcelSheet.MemberFunctionPointers.GetRowByDescriptor, Detour_GetRowByDescriptor);

        getSubRowByDescriptorHook = provider.HookFromAddress<GetSubRowByDescriptorDelegate>(
            ExcelSheet.MemberFunctionPointers.GetSubRowByDescriptor, Detour_GetSubRowByDescriptor);

        resolveIndirectionHook = provider.HookFromAddress<ResolveStringColumnIndirectionDelegate>(
            ExcelRow.MemberFunctionPointers.ResolveStringColumnIndirection, Detour_ResolveStringColumnIndirection);

        formatAddonTextApplyHook = provider.HookFromAddress<FormatAddonTextApplyDelegate>(
            RaptureTextModule.MemberFunctionPointers.FormatAddonTextApply,
            Detour_FormatAddonTextApply);
    }

    public void UpdateEngine(string engineId) => translationParser.UpdateEngine(engineId);

    public void EnableAll()
    {
        getRowByIdHook.Enable();
        getRowByIndexHook.Enable();
        getRowByDescriptorHook.Enable();
        getSubRowByDescriptorHook.Enable();
        resolveIndirectionHook.Enable();
        formatAddonTextApplyHook.Enable();
    }

    public void DisableAll()
    {
        getRowByIdHook.Disable();
        getRowByIndexHook.Disable();
        getRowByDescriptorHook.Disable();
        getSubRowByDescriptorHook.Disable();
        resolveIndirectionHook.Disable();
        formatAddonTextApplyHook.Disable();
    }

    public void Dispose()
    {
        DisableAll();
        DisposeHooks();

        translationParser.Dispose();
    }

    private void DisposeHooks()
    {
        getRowByIdHook.Dispose();
        getRowByIndexHook.Dispose();
        getRowByDescriptorHook.Dispose();
        getSubRowByDescriptorHook.Dispose();
        resolveIndirectionHook.Dispose();
        formatAddonTextApplyHook.Dispose();
    }

    private IExcelRowWrapper* Detour_GetRowById(ExcelSheet* sheet, uint rowId, uint* outErrorCode)
    {
        var result = getRowByIdHook.Original(sheet, rowId, outErrorCode);
        PopulateRowMap(result, sheet, rowId, nameof(Detour_GetRowById));
        return result;
    }

    private IExcelRowWrapper* Detour_GetRowByIndex(ExcelSheet* sheet, uint rowIndex, ExcelRowDescriptor* descriptor)
    {
        var result = getRowByIndexHook.Original(sheet, rowIndex, descriptor);
        PopulateRowMap(result, sheet, GetRowId(descriptor), nameof(Detour_GetRowByIndex));
        return result;
    }

    private IExcelRowWrapper* Detour_GetRowByDescriptor(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode)
    {
        var result = getRowByDescriptorHook.Original(sheet, descriptor, outErrorCode);
        PopulateRowMap(result, sheet, GetRowId(descriptor), nameof(Detour_GetRowByDescriptor));
        return result;
    }

    private IExcelRowWrapper* Detour_GetSubRowByDescriptor(ExcelSheet* sheet, ExcelRowDescriptor* descriptor, uint* outErrorCode)
    {
        var result = getSubRowByDescriptorHook.Original(sheet, descriptor, outErrorCode);
        PopulateRowMap(result, sheet, GetRowId(descriptor), nameof(Detour_GetSubRowByDescriptor));
        return result;
    }

    private static uint? GetRowId(ExcelRowDescriptor* descriptor)
    {
        return descriptor is null ? null : descriptor->RowId;
    }

    private byte* Detour_FormatAddonTextApply(
        RaptureTextModule* module,
        uint addonId,
        uint mode,
        void* localParameters,
        void* formatBuffer,
        void* normalizationBuffer)
    {
        var previousAddonId = currentAddonId;
        currentAddonId = addonId;

        try
        {
            return formatAddonTextApplyHook.Original(
                module,
                addonId,
                mode,
                localParameters,
                formatBuffer,
                normalizationBuffer);
        }
        finally
        {
            currentAddonId = previousAddonId;
        }
    }

    private void* Detour_ResolveStringColumnIndirection(void* columnPtr)
    {
        var result = resolveIndirectionHook.Original(columnPtr);

        if (currentAddonId is uint addonId)
        {
            currentAddonId = null;

            if (Plugin.filter.IsActive(AddonSheetName, 0) &&
                translationParser.TryGetValue(AddonSheetName, addonId, 0, out var addonTranslation))
            {
                return addonTranslation!.Pointer;
            }
        }

        if (!TryGetColumnInfo((nint)columnPtr, out var info))
            return result;

        if (Plugin.filter.IsActive(info.SheetName, info.RowId) &&
            translationParser.TryGetValue(info.SheetName, info.RowId, info.ColumnIndex, out var translation))
        {
            return translation!.Pointer;
        }

        return result;
    }

    private bool TryGetColumnInfo(nint columnPtr, out ColumnInfo info)
        => columnMap.TryGetValue(columnPtr, out info);

    private void PopulateRowMap(
        IExcelRowWrapper* wrapper,
        ExcelSheet* sheet,
        uint? rowId,
        string source)
    {
        if (!TryGetRowContext(wrapper, sheet, out var row, out var activeSheet, out var sheetName))
            return;

        if (currentAddonId is not null &&
            string.Equals(sheetName, AddonSheetName, StringComparison.Ordinal))
        {
            return;
        }

        uint resolvedRowId = rowId ?? 0;

        try
        {
            var stringColumnIndices = GetStringColumnIndices(activeSheet);
            for (uint columnIndex = 0; columnIndex < stringColumnIndices.Length; columnIndex++)
            {
                uint globalColumnIndex = stringColumnIndices[columnIndex];
                void* columnPtr = row->GetColumnPtr(globalColumnIndex);
                if (columnPtr == null)
                    continue;

                TryAddColumnToCache(
                    columnPtr,
                    source,
                    resolvedRowId,
                    columnIndex,
                    activeSheet->SheetIndex,
                    sheetName);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"[{source}] Failed to populate column cache for sheet '{sheetName}'.");
        }
    }

    private static bool TryGetRowContext(
        IExcelRowWrapper* wrapper,
        ExcelSheet* fallbackSheet,
        out ExcelRow* row,
        out ExcelSheet* activeSheet,
        out string sheetName)
    {
        row = wrapper == null ? null : wrapper->Row;
        activeSheet = row == null
            ? null
            : row->Sheet != null ? row->Sheet : fallbackSheet;
        sheetName = activeSheet == null ? string.Empty : activeSheet->SheetName.ToString();

        return row != null && activeSheet != null && !string.IsNullOrEmpty(sheetName);
    }

    private void TryAddColumnToCache(
        void* columnPtr,
        string source,
        uint rowId,
        uint columnIndex,
        uint sheetIndex,
        string sheetName)
    {
        var columnInfo = new ColumnInfo(
            Supplier: source,
            RowId: rowId,
            ColumnIndex: columnIndex,
            SheetIndex: sheetIndex,
            SheetName: sheetName);

        columnMap.TryAdd((nint)columnPtr, columnInfo);
    }

    /// <summary>
    /// Возвращает глобальные индексы текстовых колонок листа.
    /// Список строится один раз и кэшируется, чтобы PopulateRowMap не выполнял
    /// повторный поиск типа и строкового индекса для каждой колонки каждой строки.
    /// В результате схема листа обходится один раз, а последующие строки проходят
    /// только по текстовым колонкам.
    /// </summary>
    /// <remarks>
    /// stringColumnIndicesMap имеет структуру Dictionary&lt;nint, uint[]&gt;:
    /// ключом является адрес ExcelSheet, а значением — упорядоченный массив
    /// глобальных индексов только текстовых колонок. Позиция элемента в массиве
    /// одновременно является его строковым индексом: например, значение [2, 5, 8]
    /// означает, что глобальная колонка 5 является второй текстовой колонкой.
    /// </remarks>
    private uint[] GetStringColumnIndices(ExcelSheet* sheet)
    {
        nint sheetPtr = (nint)sheet;
        if (stringColumnIndicesMap.TryGetValue(sheetPtr, out var indices))
            return indices;

        indices = BuildStringColumnIndices(sheet);
        return stringColumnIndicesMap.GetOrAdd(sheetPtr, indices);
    }

    private static uint[] BuildStringColumnIndices(ExcelSheet* sheet)
    {
        var columns = sheet->ColumnDefinitionSpan;
        var stringColumnIndices = new List<uint>();
        for (uint index = 0; index < (uint)columns.Length; index++)
        {
            if (columns[(int)index].Type == (ushort)ExcelColumnType.String)
                stringColumnIndices.Add(index);
        }

        return stringColumnIndices.ToArray();
    }
}

public readonly record struct ColumnInfo(
    string Supplier,
    uint RowId,
    uint ColumnIndex,
    uint SheetIndex,
    string SheetName
);
