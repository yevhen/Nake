#region LICENSE

/*

Copyright 2013 Glenn Block, Justin Rusbatch, Filip Wojcieszyn
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 
ADD: Absolute references handling (Copyright 2013 Yevhen Bobrov :)
 
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Nake.Scripting
{
    class Preprocessor
    {
        public const string LoadString = "#load ";
        public const string UsingString = "using ";
        public const string RString = "#r ";

        string scriptDirectoryPath = "";

        public Result Process(FileInfo file)
        {
            var result = new Result();
            
            scriptDirectoryPath = file.DirectoryName;
            ParseFile(file.FullName, result);

            return result;
        }

        private void ParseFile(string path, Result result)
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

        private void ProcessLine(string currentScriptFilePath, Result result, string line, bool isBeforeCode)
        {
            if (IsUsingLine(line))
            {
                var @using = GetPath(UsingString, line);
                
                result.Namespaces.Add(@using);

                return;
            }

            if (IsRLine(line))
            {
                if (isBeforeCode)
                {
                    var reference = GetPath(RString, line);

                    if (reference.EndsWith(@".dll") || reference.EndsWith(@".exe"))
                    {
                        result.AbsoluteReferences.Add(
                            new AbsoluteReference(currentScriptFilePath, reference));

                        return;
                    }

                    result.References.Add(new AssemblyNameReference(currentScriptFilePath, reference));
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

        internal class Result
        {
            public readonly HashSet<string> Namespaces = new HashSet<string>();
            public readonly HashSet<AssemblyNameReference> References = new HashSet<AssemblyNameReference>();
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
                var lines = Namespaces.Distinct().Select(item => string.Format("using {0};", item)).ToList();

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
    }

    internal struct AbsoluteReference : IEquatable<AbsoluteReference>
    {
        public readonly string AssemblyPath;
        public readonly string ScriptFile;

        public AbsoluteReference(string scriptFile, string assemblyReference)
        {
            ScriptFile = scriptFile;

            var currentScriptDirectory = Path.GetDirectoryName(scriptFile);
            Debug.Assert(currentScriptDirectory != null);

            AssemblyPath = Path.GetFullPath(Path.Combine(currentScriptDirectory, assemblyReference));
        }

        public bool Equals(AbsoluteReference other)
        {
            return String.Equals(AssemblyPath, other.AssemblyPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is AbsoluteReference && Equals((AbsoluteReference)obj);
        }

        public override int GetHashCode()
        {
            return AssemblyPath.GetHashCode();
        }

        public override string ToString()
        {
            return AssemblyPath;
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
            return obj.AssemblyPath;
        }
    }

    internal struct AssemblyNameReference : IEquatable<AssemblyNameReference>
    {
        public readonly string AssemblyName;
        public string ScriptFile;

        public AssemblyNameReference(string scriptFile, string assemblyName)
        {
            ScriptFile = scriptFile;
            AssemblyName = assemblyName;
        }

        public bool Equals(AssemblyNameReference other)
        {
            return String.Equals(AssemblyName, other.AssemblyName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is AssemblyNameReference && Equals((AssemblyNameReference)obj);
        }

        public override int GetHashCode()
        {
            return AssemblyName.GetHashCode();
        }

        public override string ToString()
        {
            return AssemblyName;
        }

        public static bool operator ==(AssemblyNameReference left, AssemblyNameReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssemblyNameReference left, AssemblyNameReference right)
        {
            return !left.Equals(right);
        }

        public static implicit operator string(AssemblyNameReference obj)
        {
            return obj.AssemblyName;
        }
    }
}