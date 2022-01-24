using System;
using System.Linq;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    public class Options_parsing
    {
        [Test]
        public void Positional_arguments()
        {
            var options = Parse("build Debug AnyCPU");
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            
            var task = options.Tasks[0];
            Assert.That(task.Name, Is.EqualTo("build"));

            var arguments = task.Arguments;
            Assert.That(arguments.Length, Is.EqualTo(2));            
            
            Assert.That(arguments[0].IsPositional());
            Assert.That(arguments[0].Value, Is.EqualTo("Debug"));

            Assert.That(arguments[1].IsPositional());
            Assert.That(arguments[1].Value, Is.EqualTo("AnyCPU"));
        }

        [Test]
        public void Named_arguments()
        {
            var options = Parse("build configuration=Debug platform=AnyCPU");
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            
            var task = options.Tasks[0];
            Assert.That(task.Name, Is.EqualTo("build"));

            var arguments = task.Arguments;
            Assert.That(arguments.Length, Is.EqualTo(2));            
            
            Assert.That(arguments[0].IsNamed());
            Assert.That(arguments[0].Name, Is.EqualTo("configuration"));
            Assert.That(arguments[0].Value, Is.EqualTo("Debug"));

            Assert.That(arguments[1].IsNamed());
            Assert.That(arguments[1].Name, Is.EqualTo("platform"));
            Assert.That(arguments[1].Value, Is.EqualTo("AnyCPU"));
        }

        [Test]
        public void Mixed_arguments()
        {
            var options = Parse("build Debug platform=AnyCPU");            
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            
            var task = options.Tasks[0];
            Assert.That(task.Name, Is.EqualTo("build"));

            var arguments = task.Arguments;
            Assert.That(arguments.Length, Is.EqualTo(2));            
            
            Assert.That(arguments[0].IsPositional());
            Assert.That(arguments[0].Value, Is.EqualTo("Debug"));

            Assert.That(arguments[1].IsNamed());
            Assert.That(arguments[1].Name, Is.EqualTo("platform"));
            Assert.That(arguments[1].Value, Is.EqualTo("AnyCPU"));
        }

        [Test]
        public void Checks_argument_specification_order()
        {
            Assert.Throws<TaskArgumentOrderException>(
                ()=> Parse("build configuration=Debug AnyCPU"));            
        }

        [Test]
        public void Multiple_tasks_without_parameters()
        {
            var options = Parse("build ; package");
            Assert.That(options.Tasks.Count, Is.EqualTo(2));

            Assert.That(options.Tasks[0].Name, Is.EqualTo("build"));
            Assert.That(options.Tasks[1].Name, Is.EqualTo("package"));
        }

        [Test]
        public void Multiple_tasks_with_parameters()
        {
            var options = Parse("build Debug ; package version=1.0");
            Assert.That(options.Tasks.Count, Is.EqualTo(2));

            Assert.That(options.Tasks[0].Name, Is.EqualTo("build"));
            Assert.That(options.Tasks[1].Name, Is.EqualTo("package"));

            var arguments = options.Tasks[0].Arguments;
            Assert.That(arguments[0].IsPositional());
            Assert.That(arguments[0].Value, Is.EqualTo("Debug"));

            arguments = options.Tasks[1].Arguments;
            Assert.That(arguments[0].IsNamed());
            Assert.That(arguments[0].Name, Is.EqualTo("version"));
            Assert.That(arguments[0].Value, Is.EqualTo("1.0"));
        }

        [Test]
        public void Handling_of_spaces_and_equal_sign_in_argument_values()
        {
            var options = Options.Parse(new[]{"build", @"pathWithSpace=C:\Space \f.exe", @"pathWithEq=C:\Eq=\F=.exe"});
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            Assert.That(options.Tasks[0].Name, Is.EqualTo("build"));

            var argWithSpace = options.Tasks[0].Arguments[0];
            Assert.That(argWithSpace.IsNamed());
            Assert.That(argWithSpace.Name, Is.EqualTo("pathWithSpace"));
            Assert.That(argWithSpace.Value, Is.EqualTo(@"C:\Space \f.exe"));

            var argWithEqSign = options.Tasks[0].Arguments[1];
            Assert.That(argWithEqSign.IsNamed());
            Assert.That(argWithEqSign.Name, Is.EqualTo("pathWithEq"));
            Assert.That(argWithEqSign.Value, Is.EqualTo(@"C:\Eq=\F=.exe"));
        }

        [Test]
        public void Switch_style_boolean_parameters()
        {
            var options = Parse(@"build dev --beta --no-lock");
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            Assert.That(options.Tasks[0].Name, Is.EqualTo("build"));

            var postionalArg = options.Tasks[0].Arguments[0];
            Assert.That(postionalArg.IsPositional());
            Assert.That(postionalArg.Value, Is.EqualTo("dev"));

            var simpleSwitch = options.Tasks[0].Arguments[1];
            Assert.That(simpleSwitch.IsNamed());
            Assert.That(simpleSwitch.Name, Is.EqualTo("beta"));
            Assert.That(simpleSwitch.Value, Is.EqualTo("true"));

            var switchWithDashes = options.Tasks[0].Arguments[2];
            Assert.That(switchWithDashes.IsNamed());
            Assert.That(switchWithDashes.Name, Is.EqualTo("nolock"));
            Assert.That(switchWithDashes.Value, Is.EqualTo("true"));
        }

        [Test]
        public void Strips_dotnet_tool_arg_separator()
        {
            var options = Parse(@"-- build");
            Assert.That(options.Tasks.Count, Is.EqualTo(1));
            Assert.That(options.Tasks[0].Name, Is.EqualTo("build"));
        }

        static Options Parse(string commandLine)
        {
            return Options.Parse(commandLine.Split());
        }
    }
}