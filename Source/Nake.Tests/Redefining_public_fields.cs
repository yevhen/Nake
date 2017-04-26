using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Redefining_public_fields : CodeFixture
    {
        [Test]
        public void Can_redefine_any_script_level_var_any_public_field_or_constant()
        {
            Build(@"
                
                var scriptLevelVar = ""0"";

                public const string PublicConstField = ""0"";
                public static string PublicStaticField = ""0"";

                const string PrivateConstField = ""0"";
                static string PrivateStaticField = ""0"";

                class NestedClass
                {
                    public static string PublicField = ""0"";
                }

                [Task] void Test()
                {
                    Env.Var[""ScriptLevelVar""]            = scriptLevelVar;
                    Env.Var[""PublicConstField""]          = PublicConstField;
                    Env.Var[""PublicStaticField""]         = PublicStaticField;
                    Env.Var[""PrivateConstField""]         = PrivateConstField;
                    Env.Var[""PrivateStaticField""]        = PrivateStaticField;
                    Env.Var[""NestedClass.PublicField""]   = NestedClass.PublicField;
                }
            ",

            Substitute()
                .Var("ScriptLevelVar",          "1")
                .Var("PublicConstField",        "1")
                .Var("PublicStaticField",       "1")
                .Var("NestedClass.PublicField", "1")
                .Var("PrivateConstField",       "1")
                .Var("PrivateStaticField",      "1")
            );

            Invoke("Test");

            Assert.That(Env.Var["ScriptLevelVar"],          Is.EqualTo("1"));
            Assert.That(Env.Var["PublicConstField"],        Is.EqualTo("1"));
            Assert.That(Env.Var["PublicStaticField"],       Is.EqualTo("1"));
            Assert.That(Env.Var["NestedClass.PublicField"], Is.EqualTo("1"));
            Assert.That(Env.Var["PrivateConstField"],       Is.EqualTo("1"));
            Assert.That(Env.Var["PrivateStaticField"],      Is.EqualTo("1"));
        }

        [Test]
        public void But_only_when_it_is_of_a_supported_type()
        {
            Build(@"

                public static string StringField = ""0"";
                public static bool BooleanField = false;
                public static int IntField = 0;
                public static decimal UnsupportedField = 0;

                [Task] public static void Test()
                {
                    Env.Var[""StringField""] = StringField;
                    Env.Var[""BooleanField""] = BooleanField.ToString();
                    Env.Var[""IntField""] = IntField.ToString();
                    Env.Var[""UnsupportedField""] = UnsupportedField.ToString();
                }
            ",

            Substitute()
                .Var("StringField", "1")
                .Var("BooleanField", "true")
                .Var("IntField", "1")
                .Var("UnsupportedField", "1")
            );

            Invoke("Test");

            Assert.That(Env.Var["StringField"], Is.EqualTo("1"));
            Assert.That(Env.Var["BooleanField"], Is.EqualTo("True"));
            Assert.That(Env.Var["IntField"], Is.EqualTo("1"));
            Assert.That(Env.Var["UnsupportedField"], Is.EqualTo("0"));
        }

        [Test]
        public void Leaves_original_value_intact_if_type_conversion_failed()
        {
            Build(@"

                public static bool BooleanField = false;
                public static int IntField = 0;
                                                
                [Task] public static void Test()
                {
                    Env.Var[""BooleanField""] = BooleanField.ToString();
                    Env.Var[""IntField""] = IntField.ToString();
                }
            ",

            Substitute()
                .Var("BooleanField", "yes")
                .Var("IntField", ".1")
            );

            Invoke("Test");

            Assert.That(Env.Var["BooleanField"], Is.EqualTo("False"));
            Assert.That(Env.Var["IntField"],     Is.EqualTo("0"));
        }

        [Test]
        public void Names_matched_case_insensitive()
        {
            Build(@"

                public static int Field = 0;
                public static int field = 0;
                                                
                [Task] public static void Test()
                {
                    Env.Var[""PascalCaseField""] = Field.ToString();
                    Env.Var[""LowerCaseField""] = field.ToString();
                }
            ",

            Substitute()
                .Var("Field", "1")
            );

            Invoke("Test");

            Assert.That(Env.Var["PascalCaseField"], Is.EqualTo("1"));
            Assert.That(Env.Var["LowerCaseField"],  Is.EqualTo("1"));
        }

        [Test]
        public void Should_escape_when_converting_strings()
        {
            Build(@"

                public static string Path = ""path"";
                public static string Quoted = ""quoted"";

                [Task] public static void Test()
                {
                    Env.Var[""Path""] = Path;
                    Env.Var[""Quoted""] = Quoted;
                }
            ",

            Substitute()
                .Var("Path", @"C:\Tools\Nake")
                .Var("Quoted", "\"\"")
            );

            Invoke("Test");

            Assert.That(Env.Var["Path"], Is.EqualTo(@"C:\Tools\Nake"));
            Assert.That(Env.Var["Quoted"], Is.EqualTo("\"\""));
        }

        [Test]
        public void Can_correctly_match_when_multiple_declarators_are_used_within_single_field_declaration()
        {
            Build(@"

                public static int Field1 = 0, Field2 = 0;
                                                
                [Task] public static void Test()
                {
                    Env.Var[""Field1""] = Field1.ToString();
                    Env.Var[""Field2""] = Field2.ToString();
                }
            ",

            Substitute()
                .Var("Field1", "1")
                .Var("Field2", "1")
            );

            Invoke("Test");

            Assert.That(Env.Var["Field1"], Is.EqualTo("1"));
            Assert.That(Env.Var["Field2"],  Is.EqualTo("1"));
        }

        static SubstitutionsBuilder Substitute()
        {
            return new SubstitutionsBuilder();
        }

        class SubstitutionsBuilder
        {
            readonly Dictionary<string, string> substitutions = new Dictionary<string, string>();

            public SubstitutionsBuilder Var(string name, string value)
            {
                substitutions.Add(name, value);
                return this;
            }

            public static implicit operator Dictionary<string, string>(SubstitutionsBuilder builder)
            {
                return builder.substitutions;
            }
        }
    }
}