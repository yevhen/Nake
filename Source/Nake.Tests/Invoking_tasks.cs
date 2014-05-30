using System;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Invoking_tasks : CodeFixture
    {
        [Test]
        public void Simple_task_invocation()
        {
            Build(@"
            
                var counter = 0;

                [Task] void Task1() 
                {
                    Env.Var[""Task1ExecutedCount""] = (++counter).ToString();
                }

                [Task] void Task2() 
                {
                    Task1();
                    Task1();
                }
            ");

            Invoke("Task2");

            Assert.That(int.Parse(Env.Var["Task1ExecutedCount"]), Is.EqualTo(2));
        }

        [Test]
        public void Step_will_only_be_executed_once()
        {
            Build(@"
            
                var counter = 0;

                [Step] void Step1() 
                {
                    Env.Var[""Step1ExecutedCount""] = (++counter).ToString();
                }

                [Step] void Step2() 
                {
                    Step1();
                }

                [Step] void Step3() 
                {
                    Step1();
                    Step2();
                }
            ");

            Invoke("Step3");
            
            Assert.That(int.Parse(Env.Var["Step1ExecutedCount"]), Is.EqualTo(1));
        }

        [Test]
        public void Task_will_be_reexecuted_when_parameter_values_are_different()
        {
            Build(@"
            
                static int counter = 0;

                [Step] void Step1(string p) 
                {
                    Env.Var[""Step1ExecutedCount""] = (++counter).ToString();
                }

                [Step] void Step2() 
                {
                    Step1(""first time"");
                    Step1(""second time"");
                }
            ");

            Invoke("Step2");

            Assert.That(int.Parse(Env.Var["Step1ExecutedCount"]), Is.EqualTo(2));
        }

        [Test]
        public void Task_invocation_failures()
        {
            Build(@"
            
                [Task] void Task() 
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
