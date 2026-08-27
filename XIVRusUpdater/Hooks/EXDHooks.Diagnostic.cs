using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XIVRusUpdater.Hooks;

public readonly record struct CacheSnapshotEntry(string SheetName, int RowCount, int ColumnCount);

public unsafe partial class EXDHooks
{
    public CacheSnapshotEntry[] GetCacheSnapshot()
    {
        var items = columnMap.Snapshot();

        return items.GroupBy(info => info.SheetName).Select(group => new CacheSnapshotEntry(
            SheetName: group.Key,
            RowCount: group.Select(i => i.RowId).Distinct().Count(),
            ColumnCount: group.Select(i => i.ColumnIndex).Distinct().Count())).OrderByDescending(e => e.RowCount).ToArray();
    }
}
