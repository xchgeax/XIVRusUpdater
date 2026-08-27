using System;
using System.Collections.Generic;
using System.Linq;

namespace XIVRusUpdater.Utils;

public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> cache;
    private readonly LinkedList<CacheItem> lruList;
    private readonly object syncRoot = new();

    public LruCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        this.capacity = capacity;
        cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        lruList = new LinkedList<CacheItem>();
    }

    public IReadOnlyList<TValue> Snapshot()
    {
        lock (syncRoot)
        {
            return lruList.Select(item => item.Value).ToList();
        }
    }

    public int Count
    {
        get
        {
            lock (syncRoot)
                return cache.Count;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (syncRoot)
        {
            if (cache.TryGetValue(key, out var node))
            {
                value = node.Value.Value;
                lruList.Remove(node);
                lruList.AddFirst(node);
                return true;
            }

            value = default!;
            return false;
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (syncRoot)
        {
            AddCore(key, value, updateExisting: true);
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        lock (syncRoot)
        {
            return AddCore(key, value, updateExisting: false);
        }
    }

    private bool AddCore(TKey key, TValue value, bool updateExisting)
    {
        if (cache.TryGetValue(key, out var node))
        {
            if (!updateExisting)
                return false;

            node.Value = new CacheItem(key, value);
            lruList.Remove(node);
            lruList.AddFirst(node);
            return true;
        }

        if (cache.Count >= capacity)
        {
            var lastNode = lruList.Last;
            if (lastNode is not null)
            {
                cache.Remove(lastNode.Value.Key);
                lruList.RemoveLast();
            }
        }

        var cacheItem = new CacheItem(key, value);
        var newNode = new LinkedListNode<CacheItem>(cacheItem);
        lruList.AddFirst(newNode);
        cache[key] = newNode;
        return true;
    }

    private struct CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; set; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
