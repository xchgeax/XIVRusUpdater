using System;
using System.Collections.Generic;
using System.Text;

namespace XIVRusUpdater.Utils;

public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> cache;
    private readonly LinkedList<CacheItem> lruList;

    public LruCache(int capacity)
    {
        this.capacity = capacity;
        cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        lruList = new LinkedList<CacheItem>();
    }

    public bool TryGetValue(TKey key, out TValue value)
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

    public void Add(TKey key, TValue value)
    {
        if (cache.TryGetValue(key, out var node))
        {
            node.Value = new CacheItem(key, value);
            lruList.Remove(node);
            lruList.AddFirst(node);
            return;
        }

        if (cache.Count >= capacity)
        {
            var lastNode = lruList.Last;
            if (lastNode != null)
            {
                cache.Remove(lastNode.Value.Key);
                lruList.RemoveLast();
            }
        }

        var cacheItem = new CacheItem(key, value);
        var newNode = new LinkedListNode<CacheItem>(cacheItem);
        lruList.AddFirst(newNode);
        cache[key] = newNode;
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
