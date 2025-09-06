using NUnit.Framework;

namespace Nake;

[TestFixture]
class Task_body_styles : CodeFixture
{
    [Test]
    public void Expression_body_vs_block_body()
    {
        Build(@"
            
                [Nake] void ExpTask() => System.Console.WriteLine(""Expression"");

                [Nake] void ExpTriviaTask() => 
                    /*weirdly formatted*/ 
                    System.Console.WriteLine(""Expression"");

                [Nake] void BodyTask() 
                {
                   System.Console.WriteLine(""Body"");
                }

                [Nake] void BodyBSDFormattedTask() {
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