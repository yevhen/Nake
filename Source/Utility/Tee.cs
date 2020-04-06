using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Medallion.Shell;

namespace Nake
{
    /// <summary>
    /// Allow to replicates functionality of Linux tee pipe
    /// when used in conjunction with <see cref="Command"/>
    /// </summary>
    /// <seealso cref="CommandExtensions.With"/>
    public class Tee
    {
        long seq; 

        readonly ConcurrentCollection standardOutput;
        readonly ConcurrentCollection standardError;

        /// <summary>
        /// Initializes new instance of <see cref="Tee"/> class
        /// with an optional standard stream action
        /// </summary>
        /// <param name="onOutput">
        /// Callback action that will be called upon appearance of a new line
        /// in either standard output or standard error streams.
        /// </param>
        public Tee(Action<string> onOutput = null) 
            : this(onOutput, onOutput)
        {}

        /// <summary>
        /// Initializes new instance of <see cref="Tee"/> class
        /// with an optional standard output and error stream actions
        /// </summary>
        /// <param name="onStandardOutput">
        /// Callback action that will be called upon appearance
        /// of a new line standard output stream
        /// </param>
        /// <param name="onStandardError">
        /// Callback action that will be called upon appearance
        /// of a new line standard error stream
        /// </param>
        public Tee(Action<string> onStandardOutput = null, Action<string> onStandardError = null) 
            : this(-1, onStandardOutput, onStandardError)
        {}

        /// <summary>
        /// Initializes new instance of <see cref="Tee"/> class with
        /// a given number of max lines to hold in a buffer and
        /// with an optional standard output and error stream actions
        /// </summary>
        /// <param name="maxLines">
        /// Max lines to keep in buffer:
        /// <list type="bullet">
        ///     <item>-1 -keep all</item>
        ///     <item>0 - keep nothing</item>
        ///     <item>N - keep last N</item>
        /// </list>
        /// </param>
        /// <param name="onStandardOutput">
        /// Callback action that will be called upon appearance
        /// of a new line standard output stream
        /// </param>
        /// <param name="onStandardError">
        /// Callback action that will be called upon appearance
        /// of a new line standard error stream
        /// </param>
        public Tee(int maxLines = -1, Action<string> onStandardOutput = null, Action<string> onStandardError = null)
        {
            standardOutput = new ConcurrentCollection(Seq, maxLines, onStandardOutput);
            standardError = new ConcurrentCollection(Seq, maxLines, onStandardError);
        }

        long Seq() => Interlocked.Increment(ref seq);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> StandardOutput() => standardOutput.Items.Select(x => x.line).ToList();
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> StandardError() => standardError.Items.Select(x => x.line).ToList();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> Output() => 
            standardOutput.Items
                .Concat(standardError.Items)
                .OrderBy(x => x.seq)
                .Select(x => x.line)
                .ToList();
        
        /// <summary>
        /// Redirects standard streams of a given <see cref="Command"/>
        /// to <see cref="StandardOutput"/> and <see cref="StandardError"/> respectively
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns><see cref="Command"/> which could further composed or awaited</returns>
        public void Capture(Command command)
        {
            command.RedirectTo(standardOutput);
            command.RedirectStandardErrorTo(standardError);
        }

        class ConcurrentCollection : ICollection<string>
        {
            readonly BlockingCollection<(long, string)> queue;
            readonly Func<long> seq;
            readonly int maxLines;
            readonly Action<string> onAddLine;
            
            public ConcurrentCollection(Func<long> seq, int maxLines = -1, Action<string> onAddLine = null)
            {
                if (maxLines < -1)
                    throw new ArgumentOutOfRangeException(
                        nameof(maxLines), "Allowed values for maxLines are: -1, 0, N+");

                this.seq = seq;
                this.maxLines = maxLines;
                this.onAddLine = onAddLine;

                queue = maxLines > 0 
                    ? new BlockingCollection<(long, string)>(maxLines) 
                    : new BlockingCollection<(long, string)>();
            }

            public void Add(string item)
            {
                onAddLine?.Invoke(item);

                if (maxLines == 0)
                    return;

                while (!queue.TryAdd((seq(), item)))
                    queue.TryTake(out _);
            }

            public IEnumerable<(long seq, string line)> Items => queue.ToArray();

            #region Unused
            public void Clear() => throw new NotImplementedException();
            public bool Contains(string item) => throw new NotImplementedException();
            public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
            public bool Remove(string item) => throw new NotImplementedException();
            public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int Count { get; }
            public bool IsReadOnly { get; }
            #endregion
        }
    }

    /// <summary>
    /// Extension for <see cref="Command"/> class
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// Redirects standard streams of a given <see cref="Command"/>
        /// to <see cref="Tee.StandardOutput"/> and <see cref="Tee.StandardError"/> respectively
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="tee">The tee</param>
        /// <returns><see cref="Command"/> which could further composed or awaited</returns>
        public static Command With(this Command command, Tee tee)
        {
            tee.Capture(command);
            return command;
        }
    }
}