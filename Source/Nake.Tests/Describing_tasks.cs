using System;
using System.Collections.Generic;
using System.Linq;

using Nake.Magic;

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
                Assert.Throws(exceptionType, () => FindTaskDeclarations(code));
                return;
            }

            var tasks = FindTaskDeclarations(code);
            Assert.That(tasks.Single().Summary, Is.EqualTo(summary));
        }

        static IEnumerable<TaskDeclaration> FindTaskDeclarations(string code)
        {
            return new TaskDeclarationScanner().Scan(code);
        }

        static object[][] TestCases()
        {
            return new[]
            {
                TaskDeclaration(
                    @"[Nake] public static void NotDescribed() {}", ""
                ),

                TaskDeclaration(
                    @"
                    /// 
                    [Nake] public static void WhitespaceOnlyInSummary() {}", ""
                ),

                TaskDeclaration(
                    @"
                    /// described in F#-style summary doc
                    [Nake] public static void ProperlyDescribed() {}", "described in F#-style summary doc"
                ),

                TaskDeclaration(
                    @"
                    // described in simple comment style
                    [Nake] public static void InvalidXmlDoc() {}", ""
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
    }
}