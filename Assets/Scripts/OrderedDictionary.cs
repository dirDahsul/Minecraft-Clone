using System;
using System.Collections.Generic;


public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue> // Need to change it to SortedList ... does the same job.. kinda more efficient
{
    private Dictionary<TKey, TValue> dictionary;
    private List<TKey> keyList;

    public OrderedDictionary()
    {
        dictionary = new Dictionary<TKey, TValue>();
        keyList = new List<TKey>();
    }

    public void Add(TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            keyList.Add(key);
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Insert(int index, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            keyList.Insert(index, key);
        }
    }

    public bool Remove(TKey key)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary.Remove(key);
            keyList.Remove(key);
            return true;
        }

        return false;
    }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (dictionary.TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
            {
                dictionary.Remove(item.Key);
                keyList.Remove(item.Key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            dictionary.Clear();
            keyList.Clear();
        }

        public int Count
        {
            get { return keyList.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary<TKey, TValue>)dictionary).IsReadOnly; }
        }

        public bool Empty()
        {
            return keyList.Count == 0;
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The number of elements in the source dictionary exceeds the available space in the destination array.");
            }

            int i = arrayIndex;
            foreach (KeyValuePair<TKey, TValue> kvp in this)
            {
                array[i++] = kvp;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public TValue LastValue()
        {
            if (keyList.Count > 0)
            {
                TKey lastKey = keyList[keyList.Count - 1];
                return dictionary[lastKey];
            }

            //throw new InvalidOperationException("OrderedDictionary is empty.");
            return default(TValue);
            //return null;
        }
        
        public TValue FirstValue()
        {
            if (keyList.Count > 0)
            {
                TKey firstKey = keyList[0];
                return dictionary[firstKey];
            }

            //throw new InvalidOperationException("OrderedDictionary is empty.");
            return default(TValue);
            //return null;
        }

        public TKey LastKey()
        {
            if (keyList.Count > 0)
            {
                TKey lastKey = keyList[keyList.Count - 1];
                return lastKey;
            }

            //throw new InvalidOperationException("OrderedDictionary is empty.");
            return default(TKey);
            //return null;
        }

        public TKey FirstKey()
        {
            if (keyList.Count > 0)
            {
                TKey firstKey = keyList[0];
                return firstKey;
            }

            //throw new InvalidOperationException("OrderedDictionary is empty.");
            return default(TKey);
            //return null;
        }

        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { dictionary[key] = value; }
        }

        public TKey GetKey(int index)
        {
            return keyList[index];
        }

        public TValue GetValue(int index)
        {
            TKey key = keyList[index];
            return dictionary[key];
        }

        public ICollection<TKey> Keys
        {
            get { return keyList; }
        }

        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (TKey key in keyList)
            {
                yield return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
