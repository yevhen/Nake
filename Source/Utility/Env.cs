using System;
using System.Collections;
using System.Linq;

namespace Nake
{
    public class Env
    {
        public static readonly DefaultScope Var = new DefaultScope();

        public class DefaultScope : EnvironmentScope
        {
            public readonly EnvironmentScope Process = new EnvironmentScope(EnvironmentVariableTarget.Process);
            public readonly EnvironmentScope User = new EnvironmentScope(EnvironmentVariableTarget.User);
            public readonly EnvironmentScope Machine = new EnvironmentScope(EnvironmentVariableTarget.Machine);

            internal DefaultScope()
                : base(EnvironmentVariableTarget.Process)
            {}
        }
    }

    public class EnvironmentScope
    {
        readonly EnvironmentVariableTarget target;

        internal EnvironmentScope(EnvironmentVariableTarget target)
        {
            this.target = target;
        }

        public string this[string name]
        {
            get
            {
                return Defined(name)
                        ? Environment.GetEnvironmentVariable(name, target)
                        : null;
            }
            set
            {
                Environment.SetEnvironmentVariable(name, value, target);
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
