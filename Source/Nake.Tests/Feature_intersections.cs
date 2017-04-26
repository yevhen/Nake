using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Feature_intersections : CodeFixture
    {
        [Test]
        public void String_arguments_within_proxied_method_call_should_be_expanded_correctly()
        {
            Build(@"

                static string Variable = ""1"";

                [Task] public static void Task(string arg)
                {
                    Env.Var[""TaskArgumentValue""] = arg;
                }

                [Task] public static void Test()
                {
                    Task(1 + $""{Variable}"");
                }

            ");

            Invoke("Test");

            Assert.That(Env.Var["TaskArgumentValue"], Is.EqualTo("11"));
        }

        [Test]
        public void String_arguments_within_proxied_method_call_should_not_be_expanded_when_surrounded_with_string_format()
        {
            Build(@"

                static string Variable = ""1"";

                [Task] public static void Task(string arg)
                {}

                [Task] public static void Test()
                {
                    // the call below should fail in runtime with FormatException, rather than being expanded
                    Task(string.Format(""{Variable}"", """"));
                }

            ");

            var exception = Assert.Throws<TaskInvocationException>(()=> Invoke("Test"));
            Assert.That(exception.SourceException.GetType() == typeof(FormatException));
        }
    }
}