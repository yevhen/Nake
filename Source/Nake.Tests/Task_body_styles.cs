using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Task_body_styles : CodeFixture
    {
        [Test]
        public void Expression_body_vs_block_body()
        {
            Build(@"
            
                [Task] void ExpTask() => System.Console.WriteLine(""Expression"");

                [Task] void ExpTriviaTask() => 
                    /*weirdly formatted*/ 
                    System.Console.WriteLine(""Expression"");

                [Task] void BodyTask() 
                {
                   System.Console.WriteLine(""Body"");
                }

                [Task] void BodyBSDFormattedTask() {
                   System.Console.WriteLine(""Body"");
                }
            ");

            Invoke("ExpTask");
            Invoke("ExpTriviaTask");
            Invoke("BodyTask");
            Invoke("BodyBSDFormattedTask");

            Assert.Pass();
        }
    }
}
