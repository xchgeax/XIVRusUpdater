using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.Excel;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.Extentions;
public static class ExcelExtensions
{
    private static unsafe void* GetNthOfType(ExcelRow* row, int n, ExcelColumnType type )
    {
        var span = row->Sheet->ColumnDefinitionSpan;
        int found = 0;
        for(int i = 0; i < span.Length; i++) 
        {
            if (span[i].Type != (ushort)type) continue;
            if (found == n)
                return (byte*)row->Data + span[i].Offset;
            found++;
        }
        return null;
    }

    private static unsafe List<uint> GetIndexes(IExcelRowWrapper* wrapper, ExcelColumnType type)
    {
        var result = new List<uint>();
        if (wrapper == null || wrapper->Row == null || wrapper->Row->Sheet == null)
            return result;

        var span = wrapper->Row->Sheet->ColumnDefinitionSpan;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Type == (ushort)type)
                result.Add((uint)i);
        }

        return result;
    }
}
