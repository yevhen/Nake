using System;
using System.Collections;
using System.Linq;

namespace Nake
{
    public class Env
    {
        public static readonly Indexer Var = new Indexer();

        public class Indexer
        {
            public string this[string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process]
            {
                get
                {
                    return Defined(name, target)
                        ? Environment.GetEnvironmentVariable(name, target)
                        : null;
                }
                set
                {
                    Environment.SetEnvironmentVariable(name, value, target);
                }
            }
        }

        public static bool Defined(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            return Environment.GetEnvironmentVariable(name, target) != null;
        }

        public static string[] All(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            return (from DictionaryEntry entry in Environment.GetEnvironmentVariables(target)
                    select entry.Key + "=" + entry.Value).ToArray();
        }
    }
}
