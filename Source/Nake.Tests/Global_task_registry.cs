using System;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Global_task_registry : CodeFixture
    {
        [Test]
        public void Should_be_able_to_find_task_case_insensitive()
        {
            Build("[Task] public static void Test() {}");

            Assert.That(Find("test"), Is.Not.Null);
            Assert.That(Find("Test"), Is.Not.Null, "Task names are case-insensitive");

            Assert.That(Find("non-existent"), Is.Null);
        }
    }
}