using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Duplicate_tasks : CodeFixture
    {
        [Test]
        public void Overloads_are_not_allowed_and_instead_optional_parameters_should_be_used()
        {
            Assert.Throws<DuplicateTaskException>(() => Build(@"
            
                [Nake] public static void Task() {}
                [Nake] public static void Task(string s) {}

            "));
        } 

        [Test]
        public void Task_names_are_case_insensitive()
        {
            Assert.Throws<DuplicateTaskException>(() => Build(@"
            
                [Nake] public static void Task() {}
                [Nake] public static void task() {}

            "));
        } 
    }
}