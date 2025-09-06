using NUnit.Framework;

namespace Nake;

[TestFixture]
class Loading_other_scripts : CodeFixture
{
    [Test]
    public void With_load_directive()
    {
        var importedScriptPath = TempFilePath();

        BuildFile(importedScriptPath, @"                
                [Nake] void Imported() 
                { 
                    var dir = ""%NakeScriptDirectory%"";                    
                    Env.Var[""Imported_NakeScriptDirectory""] = dir;
                }
            ");

        BuildFile($@"
                #load ""{importedScriptPath}""
                [Nake] void Default(){{}}
            ");

        Invoke("Imported");

        Assert.That(Env.Var["Imported_NakeScriptDirectory"], Is.EqualTo(importedScriptPath.DirectoryName));
    }
}