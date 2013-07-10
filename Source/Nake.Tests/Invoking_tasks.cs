using System;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Invoking_tasks : CodeFixture
    {
        [Test]
        public void Task_will_only_be_executed_once()
        {
            Build(@"
            
                static int counter = 0;

                [Task] public static void Task1() 
                {
                    Env.Var[""Task1ExecutedCount""] = (++counter).ToString();
                }

                [Task] public static void Task2() 
                {
                    Task1();
                }

                [Task] public static void Task3() 
                {
                    Task1();
                    Task2();
                }
            ");

            Invoke("Task3");

            var task1ExecutedCount = int.Parse(Env.Var["Task1ExecutedCount"]);
            Assert.That(task1ExecutedCount, Is.EqualTo(1));
        }

        [Test]
        public void Task_will_be_reexecuted_when_parameter_values_are_different()
        {
            Build(@"
            
                static int counter = 0;

                [Task] public static void Task1(string p) 
                {
                    Env.Var[""Task1ExecutedCount""] = (++counter).ToString();
                }

                [Task] public static void Task2() 
                {
                    Task1(""first time"");
                    Task1(""second time"");
                }
            ");

            Invoke("Task2");

            var task1ExecutedCount = int.Parse(Env.Var["Task1ExecutedCount"]);
            Assert.That(task1ExecutedCount, Is.EqualTo(2));
        }

        [Test]
        public void Task_invocation_failures()
        {
            Build(@"
            
                [Task] public static void Task() 
                {
                    throw new ApplicationException(""crash"");
                }
            ");

            var exception = Assert.Throws<TaskInvocationException>(() => Invoke("Task"));
            Assert.That(exception.SourceException, Is.TypeOf<ApplicationException>());
            Assert.That(exception.SourceException.Message, Is.EqualTo("crash"));
        }

        [Test]
        public void Invoking_tasks_with_parameters()
        {
            Build("[Task] public static void Task(string arg1, int arg2, bool arg3 = false, int arg4 = 10) {}");

            Assert.Throws<TaskArgumentException>(() => Invoke("Task"));

            Assert.Throws<TaskArgumentException>(() => Invoke("Task",
                new TaskArgument("text")
            ));

            Assert.Throws<TaskArgumentException>(() => Invoke("Task",
                new TaskArgument("text"),
                new TaskArgument("should_be_int")
            ));

            Assert.Throws<TaskArgumentException>(() => Invoke("Task",
                new TaskArgument("text"),
                new TaskArgument("100"),
                new TaskArgument("wrong_parameter_name", "true")
            ));

            Invoke("Task",
                new TaskArgument("text"),
                new TaskArgument("100")
            );

            Invoke("Task",
                new TaskArgument("text"),
                new TaskArgument("100"),
                new TaskArgument("arg4", "1")
            );
        }
    }
}
