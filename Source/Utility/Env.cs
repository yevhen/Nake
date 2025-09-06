using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nake;

/// <summary>
/// Helper methods to deal with environment variables. Default level is Process.
/// </summary>
public static class Env
{
    /// <summary>
    /// An entry point for set of helper methods to deal with environment variables
    /// </summary>
    public static readonly Variables Var = new();

    /// <summary>
    /// An entry point for set of helper methods to deal with environment variables
    /// </summary>
    public class Variables : EnvironmentScope
    {
        /// <summary>
        /// Allows to manipulate environment variables on the user-level
        /// </summary>
        public readonly EnvironmentScope User = new(EnvironmentVariableTarget.User);

        /// <summary>
        /// Allows to manipulate environment variables on the machine-level
        /// </summary>
        public readonly EnvironmentScope Machine = new(EnvironmentVariableTarget.Machine);

        internal Variables()
            : base(EnvironmentVariableTarget.Process)
        {}
    }
}

/// <summary>
/// Contains actual methods to deal with environment variables
/// </summary>
public class EnvironmentScope : IEnumerable<KeyValuePair<string, string>>
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
    public string[] All() => this.Select(x => $"{x.Key}={x.Value}").ToArray();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        (from DictionaryEntry entry in Environment.GetEnvironmentVariables(target)
            select new KeyValuePair<string,string>((string) entry.Key, (string) entry.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}