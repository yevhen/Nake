using System;
using System.Collections.Generic;

namespace Nake
{
	static class DictionaryExtensions
	{
		public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class 
		{
			TValue result;
			return !dictionary.TryGetValue(key, out result) ? null : result;
		}
	}

	static class ArrayExtensions
	{
		public static T[] Slice<T>(this T[] source, int start)
		{
			var length = source.Length - start;
			
			if (length == 0)
				return new T[0];
			
			var slice = new T[length];
			Array.Copy(source, start, slice, 0, length);

			return slice;
		}
	}
}
