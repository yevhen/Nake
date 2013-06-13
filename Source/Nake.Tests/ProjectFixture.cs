using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

namespace Nake.Tests
{
	[TestFixture]
	public class ProjectFixture
	{
		Project project;

		[SetUp]
		public void SetUp()
		{
			project = new Project();
		}

		[Test]
		public void Should_not_allow_empty_description()
		{
			Assert.Throws<ArgumentException>(()=> Desc(""));
			Assert.Throws<ArgumentException>(() => Desc(null));
		}

		[Test]
		public void Should_not_allow_colon_in_task_name()
		{
			Assert.Throws<ArgumentException>(() => Task("some:task:with:colon"));
		}

		[Test]
		public void Should_not_allow_null_or_whitespace_only_task_names()
		{
			Assert.Throws<ArgumentException>(() => Task(null));
			Assert.Throws<ArgumentException>(() => Task(""));
			Assert.Throws<ArgumentException>(() => Task("   "));
		}

		[Test]
		public void Should_not_allow_duplicate_description_call()
		{
			Desc("description");
			Assert.Throws<DuplicateDescriptionException>(() => Desc("other description"));
		}

		[Test]
		public void Should_not_allow_duplicate_task_names()
		{
			Task("test");
			Assert.Throws<DuplicateTaskException>(() => Task("test"));
		}

		[Test]
		public void Should_fail_if_requested_target_cannot_be_found()
		{
			Assert.Throws<TaskNotFoundException>(() => Invoke("non-existent target"));
		}

		[Test]
		public void Should_execute_queued_task_only_on_run()
		{
			var executed = false;
			
			Task("test", () => {executed = true;});
			Assert.False(executed);

			Invoke("test");
			Assert.True(executed);
		}

		[Test]
		public void Should_fail_if_specified_dependency_cannot_be_found()
		{
			Task("test", new[]{"non-existent dependency"});
			Assert.Throws<TaskPrerequisiteNotFoundException>(() => Invoke("test"));
		}

		[Test]
		public void Should_chain_dependencies_as_specified()
		{
			var dependencyExecuted = false;
			
			Task("task", new[] {"dependency"}, () => { dependencyExecuted = true; });
			Task("dependency");

			Invoke("task");
			Assert.True(dependencyExecuted);
		}

		[Test]
		public void Should_be_able_to_find_tasks_case_insensitive()
		{
			var found = false;

			Task("Test", () => { found = true; });
			Invoke("test");

			Assert.True(found);
		}

		[Test]
		public void Should_support_namespace_hierarchies()
		{
			var defaultNamespaceTaskExecuted = false;
			var customNamespaceTaskExecuted = false;
			var deepNamespaceTaskExecuted = false;
			var customNamespaceScopeTaskExecuted = false;

			Task("Test", () => { defaultNamespaceTaskExecuted = true; });
			
			Namespace("Custom", () =>
			{
				Task("Test", () => { customNamespaceTaskExecuted = true; });

				Namespace("Deep", () => Task("Test", () => { deepNamespaceTaskExecuted = true; }));

				Task("Scope", () => { customNamespaceScopeTaskExecuted = true; });
			});

			Invoke("test");
			Assert.True(defaultNamespaceTaskExecuted);
			Assert.False(customNamespaceTaskExecuted);
			Assert.False(deepNamespaceTaskExecuted);

			Invoke("custom:test");
			Assert.True(customNamespaceTaskExecuted);
			Assert.False(deepNamespaceTaskExecuted);

			Invoke("custom:deep:test");
			Assert.True(deepNamespaceTaskExecuted);

			Invoke("custom:scope");
			Assert.True(customNamespaceScopeTaskExecuted);
		}

		[Test]
		public void Should_bind_unadorned_dependency_names_to_current_namespace()
		{
			var rootNamespaceDependencyExecuted = false;
			var customNamespaceDependencyExecuted = false;

			Task("Task1", () => { rootNamespaceDependencyExecuted = true; });

			Namespace("Custom", () =>
			{
				Task("Task1", () => { customNamespaceDependencyExecuted = true; });

				Task("Task2", new[]{"Task1"});
			});

			Invoke("custom:task2");

			Assert.True(customNamespaceDependencyExecuted);
			Assert.False(rootNamespaceDependencyExecuted);
		}

		[Test]
		public void Should_bind_dependency_names_starting_with_colon_to_default_implicit_namespace()
		{
			var rootNamespaceDependencyExecuted = false;
			var customNamespaceDependencyExecuted = false;

			Task("Task1", () => { rootNamespaceDependencyExecuted = true; });

			Namespace("Custom", () =>
			{
				Task("Task1", () => { customNamespaceDependencyExecuted = true; });

				Task("Task2", new[] { ":Task1" });
			});

			Invoke("custom:task2");

			Assert.False(customNamespaceDependencyExecuted);
			Assert.True(rootNamespaceDependencyExecuted);
		}

		[Test]
		public void Should_not_allow_any_other_colons_in_rooted_dependency_definitions()
		{
			Assert.Throws<ArgumentException>(()=> Task("Task", new[] { ":RootedDependency:WithAnotherColon" }));
		}

		[Test]
		public void Should_not_allow_null_or_whitespace_dependency_definitions()
		{
			Assert.Throws<ArgumentException>(() => Task("Task", new[] {""}));
			Assert.Throws<ArgumentException>(() => Task("Task", new[] {"   "}));
			Assert.Throws<ArgumentException>(() => Task("Task", new string[] {null}));
		}

		[Test]
		public void Should_fail_execution_on_Fail()
		{
			bool furtherExecutionWasAborted = true;

			Task("aborted", () => project.Abort("aborted!"));
			Task("task", new[] { "aborted" }, () => furtherExecutionWasAborted = false);

			Assert.True(furtherExecutionWasAborted);
		}

		[Test]
		public void Should_synthesize_file_task_for_dependecy_if_no_defined_task_could_be_found_but_file_exists()
		{
			File.WriteAllText("existent_file.txt", "");

			bool taskExecuted = false;
			Task("test", new[] { "existent_file.txt" }, () => { taskExecuted = true; });
			
			Invoke("test");
			Assert.True(taskExecuted);

			var synthesizedFileTask = project.Tasks["existent_file.txt"];
			
			Assert.True(synthesizedFileTask is FileTask);
			Assert.True(!synthesizedFileTask.Prerequisites.Any());
		}

		[Test]
		public void Should_fail_for_dependecy_lookup_if_no_defined_task_could_be_found_and_no_file_exists()
		{
			var taskExecuted = false;
			Task("test", new[] { "non_existent_file.txt" }, () => { taskExecuted = true; });

			Assert.Throws<TaskPrerequisiteNotFoundException>(()=> Invoke("test"));
			Assert.False(taskExecuted);
			Assert.False(project.Tasks.ContainsKey("non_existent_file.txt"));
		}

		[Test]
		public void Should_define_file_task()
		{
			var taskExecuted = false;
			Task("test", new[] { "non_existent_file.txt" }, () => { taskExecuted = true; });

			Assert.Throws<TaskPrerequisiteNotFoundException>(() => Invoke("test"));
			Assert.False(taskExecuted);
			Assert.False(project.Tasks.ContainsKey("non_existent_file.txt"));
		}

		void Desc(string text)
		{
			project.desc(text);
		}

		Task Task(string name, string[] dependencies, Action action = null)
		{
			return project.task(name, dependencies, action ?? (() => {}));
		}

		Task Task(string name, Action action = null)
		{ 
			return project.task(name, action ?? (() => {}));
		}

		public void Namespace(string name, Action define)
		{
			project.@namespace(name, define);
		}

		void Invoke(string target)
		{
			project.Invoke(target);
		}
	}
}