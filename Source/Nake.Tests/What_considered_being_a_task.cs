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
                    @"public static void NotAnnotated() {}", isTask: false
                ),                

                TaskDeclaration(
                    @"[Task] public static void GlobalTask() {}"
                ),

                TaskDeclaration(
                    @" 
                      public static class Namespace
                      {
                         [Task] public static void NamespaceTask(){}
                      }
                    "
                ),

                TaskDeclaration(
                    @" 
                    public static class Deep
                    {
                        public static class Namespace
                        {
                            [Task] public static void Task(){}
                        }
                    }
                    "
                ),

                TaskDeclaration(
                    @"[Task] public static void AllParametersAreConvertChangeTypeCompatible(
                        int p0, bool p1, string p3    
                    ){}"
                ),
  
                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public static void HasIncompatibleParam(object o) {}"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public static string NotVoid() { return null; }"
                ),

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] static void NotPublic() {}"
                ),                

                BadTaskDeclaration<TaskSignatureViolationException>(
                    @"[Task] public void NotStatic() {}"
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

                BadTaskDeclaration<TaskPlacementViolationException>(
                    @"
                    public class NotStaticClass
                    {
                        [Task] public static void BadlyPlacedTask(){}
                    }
                    "
                ),

                BadTaskDeclaration<TaskPlacementViolationException>(
                    @"
                    class NotPublicStaticClass
                    {
                        [Task] public static void AlsoBadlyPlacedTask(){}
                    }
                    "
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
