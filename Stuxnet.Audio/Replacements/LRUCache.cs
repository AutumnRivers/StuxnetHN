using System.Collections.Generic;

namespace StuxnetHN.Audio.Replacements
{
    public class LruCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> dictionary;
        private readonly LinkedList<CacheItem> linkedList;

        public LruCache(int capacity)
        {
            this.capacity = capacity;
            dictionary = new Dictionary<TKey, LinkedListNode<CacheItem>>();
            linkedList = new LinkedList<CacheItem>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out var node))
            {
                // Move the accessed item to the front of the list.
                linkedList.Remove(node);
                linkedList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                // Update an existing item's value and move it to the front.
                var node = dictionary[key];
                node.Value.Value = value;
                linkedList.Remove(node);
                linkedList.AddFirst(node);
            }
            else
            {
                if (dictionary.Count >= capacity)
                {
                    // Remove the least recently used item (the tail of the list).
                    var lastNode = linkedList.Last;
                    if (lastNode != null)
                    {
                        dictionary.Remove(lastNode.Value.Key);
                        linkedList.RemoveLast();
                    }
                }

                // Add the new item to the dictionary and the linked list.
                var newNode = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                linkedList.AddFirst(newNode);
                dictionary.Add(key, newNode);
            }
        }

        private class CacheItem
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
}
