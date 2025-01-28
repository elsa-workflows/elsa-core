namespace Elsa.Workflows;

public class ChangeTrackingDictionary<TKey, TValue>(Action onChange) : Dictionary<TKey, TValue> where TKey : notnull
{
    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        onChange();
    }

    public new bool Remove(TKey key)
    {
        var result = base.Remove(key);
        if (result) onChange();
        return result;
    }

    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            base[key] = value;
            onChange();
        }
    }
}
