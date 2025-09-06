using System.Collections.Generic;
using NUnit.Framework;

namespace Nake;

[TestFixture]
class Environment_variable_interpolation : CodeFixture
{
    static IEnumerable<TestCaseData> InliningSurroundingTestCases()
    {
        yield return new TestCaseData(@"""foo%var%""", "bar", "foobar").SetName("Text before");
        yield return new TestCaseData(@"""%var%bar""", "foo", "foobar").SetName("Text after");
        yield return new TestCaseData(@"""foo%var%baz""", "bar", "foobarbaz").SetName("In the middle of text");
        yield return new TestCaseData(@"""foo%var%baz%var%qrux""", "bar", "foobarbazbarqrux").SetName("In the middle of text, multiple times");
        yield return new TestCaseData(@"""%var%""", @"C:\Work\OSS", @"C:\Work\OSS").SetName("No surrounding");
        yield return new TestCaseData(@"""%var%""", @"C:\\Work\\OSS", @"C:\\Work\\OSS").SetName("No surrounding, double slash value");
        yield return new TestCaseData(@"@""%var%""", @"C:\Work\OSS", @"C:\Work\OSS").SetName("Verbatim empty surrounding");
        yield return new TestCaseData(@"@""%var%""", @"C:\\Work\\OSS", @"C:\\Work\\OSS").SetName("Verbatim empty surrounding, double slash value");
        yield return new TestCaseData("\"\\\"%var%\\\"\"", @"""C:\Work\OSS""", @"""""C:\Work\OSS""""").SetName("Double quotes surrounding, doubly quoted value");
        yield return new TestCaseData(@"@""""""%var%""""""", @"""C:\Work\OSS""", @"""""C:\Work\OSS""""").SetName("Verbatim double quotes surrounding, doubly quoted value");
    }

    class Constant
    {
        [Test]
        public void Evaluated_at_compile_time()
        {
            Env.Var["var"] = @"C:\Work\OSS";

            Build(@"                
                    const string inlined = ""%var%"";                    

                    [Nake] void Interpolate() => Env.Var[""Inlined""] = inlined;                    
                ");

            Env.Var["var"] = "changed";

            Invoke("Interpolate");

            Assert.That(Env.Var["Inlined"], Is.EqualTo(@"C:\Work\OSS"));
        }

        [Test]
        [Category("Slow")]
        public void NakeScriptDirectory_inlined_at_compile_time()
        {
            var path = BuildFile(@"                
                    const string inlined = ""%NakeScriptDirectory%"";

                    [Nake] void Interpolate() => Env.Var[""Constant_NakeScriptDirectory""] = inlined;                    
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Constant_NakeScriptDirectory"], Is.EqualTo(path.DirectoryName));
        }

        [Test]
        [TestCaseSource(typeof(Environment_variable_interpolation), nameof(InliningSurroundingTestCases))]
        public void Inlined_respectively_to_surroundings(string surrounding, string value, string result)
        {
            Env.Var["var"] = value;

            Build($@"                
                    const string inlined = {surrounding};                    

                    [Nake] void Interpolate() => Env.Var[""Inlined""] = inlined;                    
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Inlined"], Is.EqualTo(result));
        }

        [Test]
        public void Works_for_all_kinds_of_constant_literals()
        {
            Env.Var["var"] = "foo";

            Build(@"

                    const string fieldConst = ""%var%"";
                        
                    [Nake] 
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

            Assert.That(Env.Var["LocalConst"], Is.EqualTo("foo"));
            Assert.That(Env.Var["FieldConst"], Is.EqualTo("foo"));
            Assert.That(Env.Var["OptionalParameterDefaultValue"], Is.EqualTo("foo"));
            Assert.That(Env.Var["AnyLiteral"], Is.EqualTo("foo"));
        }

        [Test]
        public void Should_unescape_doubled_interpolation_markers()
        {
            Env.Var["whatever"] = "sigh!";

            Build(@"

                    const string esc = ""%%whatever%%"";
                    const string esc_inline = ""%%whatever%%_%whatever%_%%whatever%%"";

                    [Nake] public static void Interpolate()
                    {
                        Env.Var[""Const_DoubledDollarSign""] = esc;
                        Env.Var[""Const_DoubledDollarSignWithInline""] = esc_inline;
                    }
                        
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Const_DoubledDollarSign"], Is.EqualTo("%whatever%"));
            Assert.That(Env.Var["Const_DoubledDollarSignWithInline"], Is.EqualTo("%whatever%_sigh!_%whatever%"));
        }

        [Test]
        public void Do_not_replace_doubled_escapes_for_incomplete_surroundings()
        {
            Build(@"
                    
                    [Nake] public static void Interpolate()
                    {
                        Env.Var[""Const_StartsFromDoubledDollarSign""] = ""%%1"";
                        Env.Var[""Const_EndsWithDoubledDollarSign""] = ""1%%"";
                    }
                    
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Const_StartsFromDoubledDollarSign"], Is.EqualTo("%%1"));
            Assert.That(Env.Var["Const_EndsWithDoubledDollarSign"], Is.EqualTo("1%%"));
        }
    }

    class Runtime
    {
        [Test]
        public void Evaluated_at_run_time()
        {
            Env.Var["var"] = "foo";

            Build(@"                
                
                    [Nake] void Interpolate() 
                    { 
                        var inlined = ""%var%"";                    
                        Env.Var[""Inlined""] = inlined;
                    }
                ");

            Env.Var["var"] = @"C:\Work\OSS";

            Invoke("Interpolate");

            Assert.That(Env.Var["Inlined"], Is.EqualTo(@"C:\Work\OSS"));
        }

        [Test]
        [Category("Slow")]
        public void NakeScriptDirectory_inlined_at_compile_time()
        {
            var path = BuildFile(@"                

                    [Nake] void Interpolate() 
                    { 
                        var inlined = ""%NakeScriptDirectory%"";                    
                        Env.Var[""Runtime_NakeScriptDirectory""] = inlined;
                    }
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Runtime_NakeScriptDirectory"], Is.EqualTo(path.DirectoryName));
        }
            
        [Test]
        [TestCaseSource(typeof(Environment_variable_interpolation), nameof(InliningSurroundingTestCases))]
        [TestCaseSource(typeof(Runtime), nameof(InterpolatedSurroundingTestCases))]
        public void Inlined_respectively_to_surroundings(string surrounding, string value, string result)
        {
            Build($@"                
                
                    [Nake] void Interpolate() 
                    {{ 
                        var inlined = {surrounding};                    
                        Env.Var[""Inlined""] = inlined;
                    }}
                ");

            Env.Var["var"] = value;

            Invoke("Interpolate");

            Assert.That(Env.Var["Inlined"], Is.EqualTo(result));
        }

        static IEnumerable<TestCaseData> InterpolatedSurroundingTestCases()
        {
            yield return new TestCaseData(@"$""foo%var%{true}""", "bar", "foobarTrue").SetName("Text before");
            yield return new TestCaseData(@"$""{true}%var%bar""", "foo", "Truefoobar").SetName("Text after");
            yield return new TestCaseData(@"$""{true}foo%var%baz{false}""", "bar", "TruefoobarbazFalse").SetName("In the middle of text");
            yield return new TestCaseData(@"$""foo{true}%var%baz%var%{false}qrux""", "bar", "fooTruebarbazbarFalseqrux").SetName("In the middle of text, multiple times");
            yield return new TestCaseData(@"$""%var%""", "bar", "bar").SetName("No surrounding");
            yield return new TestCaseData(@"$@""%var%""", "bar", "bar").SetName("Verbatim empty surrounding");
            yield return new TestCaseData("\"\\\"%var%\\\"\"", "bar", "\"bar\"").SetName("Double quotes surrounding");
            yield return new TestCaseData(@"$@""""""%var%""""""", "bar", "\"bar\"").SetName("Verbatim double quotes surrounding");
        }

        [Test]
        public void Should_unescape_doubled_interpolation_markers()
        {
            Env.Var["whatever"] = "sigh!";

            Build(@"
                    
                    [Nake] public static void Interpolate()
                    {
                        Env.Var[""Soft_DoubledDollarSign""] = ""%%whatever%%"";
                        Env.Var[""Soft_DoubledDollarSignWithInline""] = ""%%whatever%%_%whatever%_%%whatever%%"";
                    }
                        
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Soft_DoubledDollarSign"], Is.EqualTo("%whatever%"));
            Assert.That(Env.Var["Soft_DoubledDollarSignWithInline"], Is.EqualTo("%whatever%_sigh!_%whatever%"));
        }

        [Test]
        public void Do_not_replace_doubled_escapes_for_incomplete_surroundings()
        {
            Build(@"
                    
                    [Nake] public static void Interpolate()
                    {
                        Env.Var[""Soft_StartsFromDoubledDollarSign""] = ""%%1"";
                        Env.Var[""Soft_EndsWithDoubledDollarSign""] = ""1%%"";
                    }
                    
                ");

            Invoke("Interpolate");

            Assert.That(Env.Var["Soft_StartsFromDoubledDollarSign"], Is.EqualTo("%%1"));
            Assert.That(Env.Var["Soft_EndsWithDoubledDollarSign"], Is.EqualTo("1%%"));
        }
    }

    [Test]
    public void Should_inline_empty_value_if_inlined_environment_variable_is_undefined()
    {
        Build(@"
                
                [Nake] public static void Interpolate()
                {
                    Env.Var[""Result""] = @""%undefined%"";
                }                    
                    
            ");
            
        Invoke("Interpolate");
            
        Assert.That(Env.Var["Result"], Is.Null);
    } 
}