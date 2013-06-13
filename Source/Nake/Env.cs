using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nake
{
	public class Env
	{
		public string this[string name]
		{
			get
			{
				if (!Vars.Contains(name))
					return null;

				return (string) Vars[name];
			}
			set
			{
				Vars[name] = value;
			}
		}

		static IDictionary Vars
		{
			get { return Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process); }
		}

		internal static IEnumerable<string> All()
		{
			return from DictionaryEntry entry in Vars 
				   select entry.Key + "=" + entry.Value;
		}
	}
}
