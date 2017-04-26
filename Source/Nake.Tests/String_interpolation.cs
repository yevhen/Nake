using System;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class String_interpolation : CodeFixture
    {
        [Test]
        public void Should_unescape_doubled_interpolation_markers()
        {
            Build(@"

                [Task] public static void Interpolate()
                {
                    Env.Var[""DoubledDollarSign""] = ""%%whatever%%"";
                    Env.Var[""DoubledCurlyBraces""] = ""{{whatever}}"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["DoubledDollarSign"], Is.EqualTo("%whatever%"));
            Assert.That(Env.Var["DoubledCurlyBraces"], Is.EqualTo("{whatever}"));
        }

        [Test]
        public void Replaces_doubled_escapes_even_for_incomplete_surroundings()
        {
            Build(@"

                [Task] public static void Interpolate()
                {
                    Env.Var[""StartsFromDoubledDollarSign""] = ""%%1"";
                    Env.Var[""EndsWithDoubledDollarSign""] = ""1%%"";
                    Env.Var[""StartsFromDoubledCurlyBrace""] = ""{{1"";
                    Env.Var[""EndsWithDoubledCurlyBrace""] = ""1}}"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["StartsFromDoubledDollarSign"], Is.EqualTo("%1"));
            Assert.That(Env.Var["EndsWithDoubledDollarSign"], Is.EqualTo("1%"));
            Assert.That(Env.Var["StartsFromDoubledCurlyBrace"], Is.EqualTo("{1"));
            Assert.That(Env.Var["EndsWithDoubledCurlyBrace"], Is.EqualTo("1}"));
        }

        [Test]
        public void Interpolating_expressions()
        {
            Build(@"

                [Task] public static void Interpolate()
                {
                    Env.Var[""Expression""] = $""{2 + 2}"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Expression"], Is.EqualTo("4"));
        }

        [Test]
        public void Native_interpolation_syntax()
        {
            Build(@"

                [Task] public static void Interpolate()
                {
                    Env.Var[""Expression""] = $""{2 + 2}"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Expression"], Is.EqualTo("4"));
        }

        [Test]
        public void Multiple_expressions_in_single_literal()
        {
            Build(@"

                [Task] public static void Interpolate()
                {
                    Env.Var[""Expression""] = $""{2 + 2}{2 + 2}"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Expression"], Is.EqualTo("44"));
        }

        [Test]
        public void Should_completely_ignore_expression_interpolation_where_constant_value_is_expected()
        {
            Build(@"

                using System.ComponentModel;

                const string fieldConst = ""{expr}"";

                [Description(""{expr}"")]
                class Attributed
                {}

                [Task]
                public static void Interpolate(string optionalParameterDefaultValue = ""{expr}"")
                {
                    const string localConst = ""{expr}"";

                    Env.Var[""LocalConst""] = localConst;
                    Env.Var[""FieldConst""] = fieldConst;
                    Env.Var[""OptionalParameterDefaultValue""] = optionalParameterDefaultValue;
                    Env.Var[""AttributeValue""] = GetAttributeValue();
                }

                static string GetAttributeValue()
                {
                    return ((DescriptionAttribute) typeof(Attributed)
                            .GetCustomAttributes(typeof(DescriptionAttribute), true)[0]).Description;
                }
            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["LocalConst"], Is.EqualTo("{expr}"));
            Assert.That(Env.Var["FieldConst"], Is.EqualTo("{expr}"));
            Assert.That(Env.Var["OptionalParameterDefaultValue"], Is.EqualTo("{expr}"));
            Assert.That(Env.Var["AttributeValue"], Is.EqualTo("{expr}"));
        }

        [Test]
        public void Should_not_interpolate_literals_surrounded_within_string_format_function()
        {
            Build(@"

                static string Variable = ""1"";

                [Task] public static void Interpolate()
                {
                    // the call below should fail in runtime with FormatException, rather than being expanded
                    Console.WriteLine(System.String.Format(""{Variable}""));
                }

            ");

            var exception = Assert.Throws<TaskInvocationException>(() => Invoke("Interpolate"));
            Assert.That(exception.SourceException.GetType() == typeof(FormatException));
        }

        [Test]
        public void Should_inline_environment_variables_in_any_literal()
        {
            Env.Var["var"] = "var";

            Build(@"

                const string fieldConst = ""%var%"";

                [Task]
                public static void Interpolate(string optionalParameterDefaultValue = ""%var%"")
                {
                    const string localConst = ""%var%"";

                    Env.Var[""LocalConst""] = localConst;
                    Env.Var[""FieldConst""] = fieldConst;
                    Env.Var[""OptionalParameterDefaultValue""] = optionalParameterDefaultValue;
                    Env.Var[""AnyLiteral""] = ""%var%"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["LocalConst"], Is.EqualTo("var"));
            Assert.That(Env.Var["FieldConst"], Is.EqualTo("var"));
            Assert.That(Env.Var["OptionalParameterDefaultValue"], Is.EqualTo("var"));
            Assert.That(Env.Var["AnyLiteral"], Is.EqualTo("var"));
        }

        [Test]
        public void Verbatim_strings_are_respected()
        {
            Env.Var["var"] = @"C:\Tools\Nake";

            Build(@"

                const string RootPath = ""%var%\\Root"";
                const string RootPathVerbatim = @""%var%\Root"";

                [Task]
                public static void Interpolate()
                {
                    var outputPath = $""{RootPath}\\Output"";
                    var outputPathVerbatim = $@""{RootPath}\Output"";

                    Env.Var[""RootPath""] = RootPath;
                    Env.Var[""RootPathVerbatim""] = RootPathVerbatim;
                    Env.Var[""OutputPath""] = outputPath;
                    Env.Var[""OutputPathVerbatim""] = outputPathVerbatim;
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["RootPath"], Is.EqualTo(@"C:\Tools\Nake\Root"));
            Assert.That(Env.Var["RootPathVerbatim"], Is.EqualTo(@"C:\Tools\Nake\Root"));
            Assert.That(Env.Var["OutputPath"], Is.EqualTo(@"C:\Tools\Nake\Root\Output"));
            Assert.That(Env.Var["OutputPathVerbatim"], Is.EqualTo(@"C:\Tools\Nake\Root\Output"));
        }

        [Test]
        public void Quotes_are_respected_as_well()
        {
            Env.Var["var"] = @"C:\Tools\Nake";

            Build(@"

                [Task]
                public static void Interpolate()
                {
                    Env.Var[""Quotes""] = ""\""%var%\"""";
                    Env.Var[""QuotesVerbatim""] = @""""""%var%"""""";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Quotes"], Is.EqualTo(@"""C:\Tools\Nake"""));
            Assert.That(Env.Var["QuotesVerbatim"], Is.EqualTo(@"""C:\Tools\Nake"""));
        }

        [Test]
        public void Should_check_whether_expression_has_valid_syntax()
        {
            Assert.Throws<NakeException>(()=> Build(@"

                const int I = 1;

                [Task] public static void Interpolate()
                {
                    Console.WriteLine($@""{I\1}"");
                }

            "));
        }

        [Test]
        public void Should_check_whether_expression_return_type_is_void()
        {
            Assert.Throws<NakeException>(()=> Build(@"

                [Task] public static void Task()
                {}

                [Task] public static void Interpolate()
                {
                    Console.WriteLine($""{Task()}"");
                }

            "));
        }

        [Test]
        public void Should_check_whether_expression_could_be_resolved()
        {
            Assert.Throws<NakeException>(()=> Build(@"

                [Task] public static void Interpolate1()
                {
                    Console.WriteLine($""{Task()}"");
                }

            "));
        }

        [Test]
        public void Should_correctly_resolve_expression_when_used_as_argument_to_step()
        {
            Build(@"

                const string RootPath = @""C:\Temp"";
                const string OutputPath = RootPath + @""\Output"";

                [Task] void Interpolate()
                {
                    var packagePath = OutputPath + @""\Package"";
                    var releasePath = packagePath + @""\Release"";

                    SomeStep($@""{packagePath}\Debug"");
                }

                [Step] void SomeStep(string path)
                {
                    Env.Var[""Result""] = path;
                }

            ");

            Assert.DoesNotThrow(() => Invoke("Interpolate"));
            Assert.That(Env.Var["Result"], Is.EqualTo(@"C:\Temp\Output\Package\Debug"));
        }

        [Test]
        public void Should_inline_UNDEFINED_value_if_inlined_environment_variable_is_undefined()
        {
            Build(@"
                
                [Task] public static void Interpolate()
                {
                    Env.Var[""Result""] = @""%undefined%"";
                }

            ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Result"], Is.EqualTo("?UNDEFINED?"));
        }
    }
}