using System;
using System.Collections;
using System.Linq;

namespace Nake
{
    public class Env
    {
        public static readonly EnvironmentScope Process = new EnvironmentScope(EnvironmentVariableTarget.Process);
        public static readonly EnvironmentScope User = new EnvironmentScope(EnvironmentVariableTarget.User);
        public static readonly EnvironmentScope Machine = new EnvironmentScope(EnvironmentVariableTarget.Machine);
        
        public static readonly Indexer Var = new Indexer();

        public class Indexer
        {
            public string this[string name]
            {
                get { return Process.Var[name]; }
                set { Process.Var[name] = value; }
            }
        }

        public static bool Defined(string name)
        {
            return Process.Defined(name);
        }

        public static string[] All()
        {
            return Process.All();
        }
    }

    public class EnvironmentScope
    {
        readonly EnvironmentVariableTarget target;

        internal EnvironmentScope(EnvironmentVariableTarget target)
        {
            this.target = target;
            Var = new Indexer(this);
        }

        public readonly Indexer Var;

        public class Indexer
        {
            readonly EnvironmentScope scope;

            internal Indexer(EnvironmentScope scope)
            {
                this.scope = scope;
            }

            public string this[string name]
            {
                get
                {
                    return scope.Defined(name)
                        ? Environment.GetEnvironmentVariable(name, scope.target)
                        : null;
                }
                set
                {
                    Environment.SetEnvironmentVariable(name, value, scope.target);
                }
            }
        }

        public bool Defined(string name)
        {
            return Environment.GetEnvironmentVariable(name, target) != null;
        }

        public string[] All()
        {
            return (from DictionaryEntry entry in Environment.GetEnvironmentVariables(target)
                    select entry.Key + "=" + entry.Value).ToArray();
        }       
    }
}
