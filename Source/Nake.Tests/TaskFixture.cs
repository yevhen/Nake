using System;

using NUnit.Framework;
using Nake.Tests.Testing;

namespace Nake.Tests
{
	[TestFixture]
	public class TaskFixture
	{
		Project project;

		[SetUp]
		public void SetUp()
		{
			project = new Project();
		}
		
		[Test]
		public void Should_only_actually_invoke_delegate_on_Execute()
		{
			var executed = false;
			
			Create(() => { executed = true; });
			
			Assert.That(executed, Is.False);
		}

		[Test]
		public void Should_only_be_executed_once()
		{
			var executed = 0;
			var task = Create(() => { executed++; });
			
			task.Invoke();
			Assert.That(executed, Is.EqualTo(1));

			task.Invoke();
			Assert.That(executed, Is.EqualTo(1));
		}

		[Test]
		public void Should_pass_itself_to_lambda()
		{
			Task passed = null;

			var task = Create(t =>
			{
				passed = t;
			});

			task.Invoke();
			Assert.That(passed, Is.SameAs(task));
		}

		[Test]
		public void Should_not_allow_adding_same_prerequisite_more_than_once()
		{
			var task = Create();

			task.AddPrerequisite("p");

			Assert.Throws<DuplicatePrerequisiteException>(() => task.AddPrerequisite("p"));
		}

		[Test]
		public void Should_invoke_all_prerequisite_tasks()
		{
			var task1Executed = false;
			var task2Executed = false;
			var task3Executed = false;

			var task1 = Create("task1", () =>
			{
				task1Executed = true;
				Assert.True(task2Executed);
				Assert.True(task3Executed);
			});

			var task2 = Create("task2", () =>
			{
				task2Executed = true;
				Assert.False(task1Executed);
				Assert.True(task3Executed);
			});
			
			Create("task3", () =>
			{
				task3Executed = true;

				Assert.False(task1Executed);
				Assert.False(task2Executed);
			});

			task1.AddPrerequisite("task2");
			task2.AddPrerequisite("task3");

			task1.Invoke();
		}

		[Test]
		public void Should_not_execute_task_if_not_needed()
		{
			var taskExecuted = false;
			var taskPrerequisiteExecuted = false;

			var prerequisite = Create(() => { taskPrerequisiteExecuted = true; });

			var task = CreateMock(() => { taskExecuted = true; });
			task.AddPrerequisite(prerequisite.Key);

			task.Needed = false;
			task.Invoke();

			Assert.True(taskPrerequisiteExecuted, 
				"Prerequisites should be invoked no matter whether current task is needed or not");

			Assert.False(taskExecuted);
		}

		[Test]
		public void Should_be_able_to_detect_cyclic_dependencies()
		{
			/*
			            t4
			        /   |
			     /     t3							 
			  /     /    \
			 t1           t2
			 
			 */

			var t1 = Create("t1");
			var t2 = Create("t2");
			var t3 = Create("t3");
			var t4 = Create("t4");

			t1.AddPrerequisite("t3");
			t1.AddPrerequisite("t4");
			t2.AddPrerequisite("t3");
			t3.AddPrerequisite("t4");

			Assert.DoesNotThrow(t1.Invoke);

			t1.Reenable();
			t2.Reenable();
			t3.Reenable();
			t4.Reenable();

			t4.AddPrerequisite("t1");
			Assert.Throws<CyclicDependencyException>(t1.Invoke);
		}

		MockTask CreateMock(Action action)
		{
			var mock = new MockTask(project, Scope.Root, action);

			project.Define(mock, mock.Key);

			return mock;
		}

		Task Create(string name = null)
		{
			return project.task(name ?? "task", t => {});
		}

		Task Create(Action action)
		{
			return project.task("task", t => action());
		}

		Task Create(Action<Task> action)
		{
			return project.task("task", action);
		}

		Task Create(string name, Action action)
		{
			return project.task(name, action);
		}
	}
}
