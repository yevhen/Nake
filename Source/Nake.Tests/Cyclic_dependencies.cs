using System;

using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Cyclic_dependencies : CodeFixture
    {
        [Test]
        public void Recursive_call()
        {
            Assert.Throws<RecursiveTaskCallException>(() => Build(@"
            
                [Task] public static void Task() 
                {
                    Task();
                }

            "));
        }

        [Test]
        public void Simplest_case_no_intermediaries()
        {
            Assert.Throws<CyclicDependencyException>(() => Build(@"
            
                [Task] public static void Task1() 
                {
                    Task2();
                }

                [Task] public static void Task2() 
                {
                    Task1();
                }

            "));
        }

        [Test]
        public void Via_intermediaries()
        {
            Assert.Throws<CyclicDependencyException>(() => Build(@"
            
                [Task] public static void Task1() 
                {
                    Task2();
                }

                [Task] public static void Task2() 
                {
                    Task3();
                }

                [Task] public static void Task3() 
                {
                    Task1();
                }

            "));
        }
    }
}