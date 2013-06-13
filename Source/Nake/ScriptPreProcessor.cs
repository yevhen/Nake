#region LICENSE

/*

Copyright 2013 Glenn Block, Justin Rusbatch, Filip Wojcieszyn
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language 
 governing permissions and limitations under the License.
 
ADD: Absolute references handling
(Copyright 2013 Yevhen Bobrov :)
 
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nake
{
	struct AbsoluteReference : IEquatable<AbsoluteReference>
	{
		readonly string path;

		public AbsoluteReference(string scriptFile, string assemblyReference)
		{
			var currentScriptDirectory = Path.GetDirectoryName(scriptFile);
			path = Path.GetFullPath(Path.Combine(currentScriptDirectory, assemblyReference));
		}

		public bool Equals(AbsoluteReference other)
		{
			return string.Equals(path, other.path);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			return obj is AbsoluteReference && Equals((AbsoluteReference) obj);
		}

		public override int GetHashCode()
		{
			return path.GetHashCode();
		}

		public override string ToString()
		{
			return path;
		}

		public static bool operator ==(AbsoluteReference left, AbsoluteReference right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AbsoluteReference left, AbsoluteReference right)
		{
			return !left.Equals(right);
		}

		public static implicit operator string(AbsoluteReference obj)
		{
			return obj.path;
		}
	}

	class ScriptPreProcessorResult
	{
		public readonly HashSet<string> Usings = new HashSet<string>();
		public readonly HashSet<string> References = new HashSet<string>();
		public readonly HashSet<AbsoluteReference> AbsoluteReferences = new HashSet<AbsoluteReference>();
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
			var lines = Usings.Distinct().Select(item => string.Format("using {0};", item)).ToList();

			if (lines.Count == 0)
				return;

			code.AppendLine(string.Join(Environment.NewLine, lines));
			code.AppendLine(); // Insert a blank separator line
		}

		void AppendBody(StringBuilder code)
		{
			code.Append(string.Join(Environment.NewLine, Body));
		}
	}

    class ScriptPreProcessor
    {
		public const string LoadString = "#load ";
		public const string UsingString = "using ";
		public const string RString = "#r ";

	    string scriptDirectoryPath = "";

        public ScriptPreProcessorResult ProcessFile(string path)
        {
			var result = new ScriptPreProcessorResult();
			
			scriptDirectoryPath = Path.GetDirectoryName(path);
			ParseFile(path, result);

	        return result;
        }

		private void ParseFile(string path, ScriptPreProcessorResult result)
        {
            var fileLines = File.ReadAllLines(path).ToList();

            InsertLineDirective(path, fileLines);

            var codeIndex = fileLines.FindIndex(IsNonDirectiveLine);

            for (var index = 0; index < fileLines.Count; index++)
            {
                ProcessLine(path, result, fileLines[index], index < codeIndex || codeIndex < 0);
            }

            result.LoadedScripts.Add(path);
        }

        private static void InsertLineDirective(string path, List<string> fileLines)
        {
            var bodyIndex = fileLines.FindIndex(line => IsNonDirectiveLine(line) && !IsUsingLine(line));
            if (bodyIndex == -1) 
				return;

            var directiveLine = string.Format("#line {0} \"{1}\"", bodyIndex + 1, path);
            fileLines.Insert(bodyIndex, directiveLine);
        }

		private void ProcessLine(string currentScriptFilePath, ScriptPreProcessorResult result, string line, bool isBeforeCode)
        {
            if (IsUsingLine(line))
            {
                var @using = GetPath(UsingString, line);
				
				result.Usings.Add(@using);

                return;
            }

            if (IsRLine(line))
            {
                if (isBeforeCode)
                {
                    var reference = GetPath(RString, line);

	                if (reference.Contains(@"\"))
	                {
		                result.AbsoluteReferences.Add(
							new AbsoluteReference(currentScriptFilePath, reference));

						return;
	                }

                    result.References.Add(reference);
                }

                return;
            }

            if (IsLoadLine(line))
            {
                if (isBeforeCode)
                {
                    var filePath = GetPath(LoadString, line);
                 
					if (!result.LoadedScripts.Contains(filePath))
                        ParseFile(Path.Combine(scriptDirectoryPath, filePath), result);
                }

                return;
            }

            // If we've reached this, the line is part of the body...
            result.Body.Add(line);
        }

		public static bool IsNonDirectiveLine(string line)
		{
			return !IsRLine(line) && !IsLoadLine(line) && line.Trim() != string.Empty;
		}

		public static bool IsUsingLine(string line)
		{
			return line.TrimStart(' ').StartsWith(UsingString) && !line.Contains("{") && line.Contains(";");
		}

		public static bool IsRLine(string line)
		{
			return line.TrimStart(' ').StartsWith(RString);
		}

		public static bool IsLoadLine(string line)
		{
			return line.TrimStart(' ').StartsWith(LoadString);
		}

		public static string GetPath(string replaceString, string line)
		{
			return line.Trim(' ').Replace(replaceString, string.Empty).Replace("\"", string.Empty).Replace(";", string.Empty);
		}
    }
}