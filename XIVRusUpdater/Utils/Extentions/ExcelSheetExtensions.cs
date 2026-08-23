using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils.Extentions;

public static unsafe class ExcelSheetExtensions
{
    extension(in ExcelSheet sheet)
    {
        public int ToStringColumnIndex(uint globalColumnIndex)
        {
            var columns = sheet.ColumnDefinitionSpan;

            if (globalColumnIndex >= (uint)columns.Length)
                return -1;

            if (columns[(int)globalColumnIndex].Type != (ushort)ExcelColumnType.String)
                return -1;

            int stringIndex = 0;
            for (int i = 0; i < globalColumnIndex; i++)
            {
                if (columns[i].Type == (ushort)ExcelColumnType.String)
                    stringIndex++;
            }

            return stringIndex;
        }

        public int ToGlobalColumnIndex(int stringColumnIndex)
        {
            var columns = sheet.ColumnDefinitionSpan;
            int counted = 0;

            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Type != (ushort)ExcelColumnType.String)
                    continue;

                if (counted == stringColumnIndex)
                    return i;

                counted++;
            }

            return -1;
        }
    }
}
