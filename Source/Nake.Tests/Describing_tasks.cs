using System;
using System.Linq;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Describing_tasks : CodeFixture
    {
        [Test]
        [TestCaseSource("TestCases")]
        public void Verify(string code, string summary, Type exceptionType)
        {
            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Build(code));
                return;
            }
            
            Build(code);

            Assert.That(Tasks.Single().Summary, Is.EqualTo(summary));
        }

        public object[][] TestCases()
        {
            return new[]
            {
                TaskDeclaration(
                    @"[Task] public static void NotDescribed() {}", ""
                ),

                TaskDeclaration(
                    @"
                    /// <summary>   </summary>
                    [Task] public static void WhitespaceOnlyInSummary() {}", ""
                ),

                TaskDeclaration(
                    @"
                    /// <summary> described in xml doc tag </summary>
                    [Task] public static void ProperlyDescribed() {}", "described in xml doc tag"
                ),

                BadTaskDeclaration<InvalidXmlDocumentationException>(
                    @"
                    /// <summary> described in xml doc tag
                    [Task] public static void InvalidXmlDoc() {}"
                )
            };
        }

        static object[] TaskDeclaration(string code, string summary)
        {
            return new object[]
            {
                code, summary, null
            };
        }

        static object[] BadTaskDeclaration<TException>(string code) where TException : Exception
        {
            return new object[]
            {
                code, null, typeof(TException)
            };
        }
    }
}