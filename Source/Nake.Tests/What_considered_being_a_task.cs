using System;
using System.Linq;
using NUnit.Framework;

namespace Nake;

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

        Assert.That(Tasks.Count(), Is.EqualTo(isTask ? 1 : 0));
    }

    static object[][] TestCases()
    {
        return
        [
            TaskDeclaration(
                @"void NotAnnotated() {}", isTask: false
            ),                

            TaskDeclaration(
                @"[Nake] void GlobalTask() {}"
            ),

            TaskDeclaration(
                @"[Nake] async void GlobalTask() {}"
            ),
                
            TaskDeclaration(
                @"[Nake] async Task GlobalTask() {}"
            ),

            TaskDeclaration(
                @" 
                      class Namespace
                      {
                         [Nake] void NamespaceTask(){}
                      }
                    "
            ),

            TaskDeclaration(
                @" 
                    class Deep
                    {
                        class Namespace
                        {
                            [Nake] void Task(){}
                        }
                    }
                    "
            ),

            TaskDeclaration(
                @"[Nake] static void StaticPrivate() {}"
            ),                

            TaskDeclaration(
                @"[Nake] public static void StaticPublic() {}"
            ),                

            TaskDeclaration(
                @"
                    class InternalClass
                    {
                        [Nake] static void PrivateStaticTask(){}
                    }
                    "
            ),

            TaskDeclaration(
                @"
                    class InternalClass
                    {
                        [Nake] public static void PublicStaticTask(){}
                    }
                    "
            ),

            TaskDeclaration(
                @"[Nake] void AllParametersAreConvertChangeTypeCompatible(
                        int p0, bool p1, string p3    
                    ){}"
            ),

            TaskDeclaration(
                @"                    
                    enum Days {Sat, Sun, Mon, Tue, Wed, Thu, Fri};
                    
                    [Nake] void EnumsCouldBeUsedAsParameters(
                        Days days
                    ){}"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] void HasIncompatibleParam(object o) {}"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] string NotVoid() { return null; }"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] public static void Generic<T>() {}"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] public static void HasOutParameters(out int p) { p = 1;}"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] public static void HasRefParameters(ref int p) {}"
            ),

            BadTaskDeclaration<TaskSignatureViolationException>(
                @"[Nake] void DuplicateParams(int p, int P) {}"
            )
        ];
    }

    static object[] TaskDeclaration(string code, bool isTask = true)
    {
        return
        [
            code, isTask, null
        ];
    }

    static object[] BadTaskDeclaration<TException>(string code) where TException : Exception
    {
        return
        [
            code, false, typeof(TException)
        ];
    }
}