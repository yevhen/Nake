using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nake.Scripting
{
    class PreprocessorResult
    {
        public readonly HashSet<string> Namespaces = new HashSet<string>();
        public readonly HashSet<AssemblyNameReference> References = new HashSet<AssemblyNameReference>();
        public readonly HashSet<AssemblyAbsoluteReference> AbsoluteReferences = new HashSet<AssemblyAbsoluteReference>();
        public readonly List<string> LoadedScripts = new List<string>();
        public readonly List<string> Body = new List<string>();

        public string Code()
        {
            var code = new StringBuilder();

            AppendUsings(code);
            AppendBody(code);

            return code.ToString();
        }

        void AppendUsings(StringBuilder code)
        {
            var lines = Namespaces.Distinct().Select(item => String.Format("using {0};", item)).ToList();

            if (lines.Count == 0)
                return;

            code.AppendLine(String.Join(Environment.NewLine, lines));
            code.AppendLine(); // Insert a blank separator line
        }

        void AppendBody(StringBuilder code)
        {
            code.Append(String.Join(Environment.NewLine, Body));
        }
    }
}