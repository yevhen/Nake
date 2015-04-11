using System;
using System.Linq;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class What_considered_being_a_task : CodeFixture
    {
        [Test]
        [TestCaseSource("TestCases")]
        public void Verify(string code, bool isTask, Type exceptionType)
        {
            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Build(code));
                return;
            }
            
            Build(code);

            Assert.AreEqual(Tasks.Count(), isTask ? 1 : 0);
        }

        public object[][] TestCases()
        {
            return new[]
            {
                TaskDeclaration(
                    @"void NotAnnotated() {}", isTask: false
                ),                

                TaskDeclaration(
                    @"[Task] void GlobalTask() {}"
                ),

                TaskDeclaration(
                    @" 
                      class Namespace
                      {
                         [Task] void NamespaceTask(){}
                      }
                    "
                ),

                TaskDeclaration(
                    @" 
                    class Deep
                    {
                        class Namespace
                        {
                            [Task] void Task(){}
                        }
                    }
                    "
                ),

                TaskDeclaration(
                    @"[Task] static void StaticPrivate() {}"
                ),                

                TaskDeclaration(
                    @"[Task] public static void StaticPublic() {}"
                ),                

                TaskDeclaration(
                    @"
                    class InternalClass
                    {
                        [Task] static void PrivateStaticTask(){}
                    }
                    "
                ),

                TaskDeclaration(
                    @"
                    class InternalClass
                    {
                        [Task] public static void PublicStaticTask(){}
                    }
                    "
                ),

                TaskDeclaration(
                    @"[Task] void AllParametersAreConvertChangeTypeCompatible(
                        int p0, bool p1, string p3    
                    ){}"
                ),

                TaskDeclaration(
                    @"                    
                    enum Days {Sat, Sun, Mon, Tue, Wed, Thu, Fri};
                    
                    [Task] void EnumsCouldBeUsedAsParameters(
                        Days days
                    ){}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] void HasIncompatibleParam(object o) {}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] string NotVoid() { return null; }"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public static void Generic<T>() {}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public static void HasOutParameters(out int p) { p = 1;}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public static void HasRefParameters(ref int p) {}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] void DuplicateParams(int p, int P) {}"
                )
            };
        }

        static object[] TaskDeclaration(string code, bool isTask = true)
        {
            return new object[]
            {
                code, isTask, null
            };
        }

        static object[] BadTaskDeclaration<TException>(string code) where TException : Exception
        {
            return new object[]
            {
                code, false, typeof(TException)
            };
        }
    }
}
