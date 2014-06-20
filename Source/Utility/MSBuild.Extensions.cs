using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nake
{
    static class MSBuildExtensions
    {
        public static string[] AsStrings(this IEnumerable<ITaskItem> sequence)
        {
            return sequence.Select(x => x.ItemSpec).ToArray();
        }

        public static ITaskItem[] AsTaskItems(this IEnumerable<string> sequence)
        {
            return sequence.Select(AsTaskItem).ToArray();
        }

        public static ITaskItem AsTaskItem(this string s)
        {
            return new TaskItem(s);
        }
    }
}
