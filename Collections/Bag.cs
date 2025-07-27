using System.Collections;
using System.Collections.Generic;

namespace AviaryModules.Collections;

public class Bag<T> : Dictionary<T, int>, IEnumerable<T>
{
    public void Add(T item)
    {
        if(ContainsKey(item))
            this[item]++;
        else
            Add(item, 1);
    }

    public void AddRange(IEnumerable<T> collection)
    {
        foreach(var item in collection)
            Add(item);
    }

    public bool Decrease(T item)
    {
        if (!ContainsKey(item)) return true;
        this[item]--;
        return this[item] == 0 && Remove(item);
    }

    public new IEnumerator<T> GetEnumerator()
    {
        return this.Keys.GetEnumerator();
    }
}