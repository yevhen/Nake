using System;
using System.Collections.Generic;

namespace Nake
{
	class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			return x.ToLower() == y.ToLower();
		}

		public int GetHashCode(string obj)
		{
			return obj.ToLower().GetHashCode();
		}
	}
}
