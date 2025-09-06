using System;
using System.Collections.Generic;

namespace Nake;

static class DictionaryExtensions
{
    public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
    {
        TValue result;
        return !dictionary.TryGetValue(key, out result) ? null : result;
    }
}
