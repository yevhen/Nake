using System;
using System.Collections;
using System.Linq;

namespace Nake
{
    /// <summary>
    /// Helper methods to deal with environment variables. Default level is Process.
    /// </summary>
    public static class Env
    {
        /// <summary>
        /// An entry point for set of helper methods to deal with environment variables
        /// </summary>
        public static readonly Variables Var = new Variables();

        /// <summary>
        /// An entry point for set of helper methods to deal with environment variables
        /// </summary>
        public class Variables : EnvironmentScope
        {
            /// <summary>
            /// Allows to manipulate environment variables on the user-level
            /// </summary>
            public readonly EnvironmentScope User = new EnvironmentScope(EnvironmentVariableTarget.User);

            /// <summary>
            /// Allows to manipulate environment variables on the machine-level
            /// </summary>
            public readonly EnvironmentScope Machine = new EnvironmentScope(EnvironmentVariableTarget.Machine);

            internal Variables()
                : base(EnvironmentVariableTarget.Process)
            {}
        }
    }

    /// <summary>
    /// Contains actual methods to deal with environment variables
    /// </summary>
    public class EnvironmentScope
    {
        readonly EnvironmentVariableTarget target;

        internal EnvironmentScope(EnvironmentVariableTarget target) => 
            this.target = target;

        /// <summary>
        /// Gets or sets the environment variable with the specified name.
        /// </summary>
        /// <value> The string value. </value>
        /// <param name="name">The variable name.</param>
        /// <returns>Value or <c>null</c> if environment variable does not exists</returns>
        public string this[string name]
        {
            get => Defined(name) ? Environment.GetEnvironmentVariable(name, target) : null;
            set => Environment.SetEnvironmentVariable(name, value, target);
        }

        /// <summary>
        /// Checks whether there is an environment variable with the specified name is defined (exists).
        /// </summary>
        /// <param name="name">The name of variable.</param>
        /// <returns><c>true</c> if defined, <c>false</c> otherwise</returns>
        public bool Defined(string name) => Environment.GetEnvironmentVariable(name, target) != null;

        /// <summary>
        /// Returns all environment variables as name=value pairs
        /// </summary>
        /// <returns>Array of environment variable pairs</returns>
        public string[] All() =>
            (from DictionaryEntry entry in Environment.GetEnvironmentVariables(target)
             select entry.Key + "=" + entry.Value).ToArray();
    }
}
