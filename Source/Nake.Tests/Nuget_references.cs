using NUnit.Framework;

namespace Nake
{
    [TestFixture]
    class Nuget_references : CodeFixture
    {
        [Test]
        public void Nuget_references_are_resolved_via_dotnet_script()
        {
            BuildFile(@"
                
                #r ""nuget: Streamstone, 2.3.0""

                using Streamstone;

                [Nake] void Test() 
                {
                    Env.Var[""ResolvedShard""] = Shard.Resolve(""A"", 10).ToString();
                }
            ");

            Invoke("Test");

            Assert.That(Env.Var["ResolvedShard"], Is.EqualTo("5"));
        }
    }
}