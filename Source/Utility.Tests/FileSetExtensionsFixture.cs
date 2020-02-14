using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Nake.Utility
{
    [TestFixture]
    public class FileSetExtensionsFixture
    {
        FileSet files;

        [SetUp]
        public void SetUp()
        {
            Location.CurrentDirectory = BaseDirectory;

            files = new FileSet();
        }

        [Test]
        public void Mirroring()
        {
            files.Include(@"A");
            files.Include(@"B");

            var destination = Path.Combine(BaseDirectory(), @"R");
            var result = files.Mirror(destination);

            var expected = new List<string>
            {
                Path.Combine(destination, @"A"),
                Path.Combine(destination, @"B"),
            };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test] public void Flattening()
        {
            files.Include(@"A\**\*.lg");

            var destination = Path.Combine(BaseDirectory(), @"R");
            var result = files.Flatten(destination);

            var expected = new List<string>
            {
                Path.Combine(destination, @"A3.lg"),
                Path.Combine(destination, @"AC3.lg"),
            };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        [Test] public void Transforming()
        {
            files.Include(@"**\*.lg");
            
            var destination = Path.Combine(BaseDirectory(), @"R");

            var result = files.Transform( 
                x => Path.Combine(destination, x.RecursivePath, x.Name + ".tmp"));

            var expected = new List<string>
            {
                Path.Combine(destination, "A", "A3.tmp"),
                Path.Combine(destination, "A", "C", "AC3.tmp"),
                Path.Combine(destination, "B", "B3.tmp"),
                Path.Combine(destination, "B", "C", "BC3.tmp"),
            };

            Assert.That(result, Is.EquivalentTo(expected));
        }

        static string BaseDirectory()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(assemblyLocation != null);

            return Path.Combine(assemblyLocation, "Testing", "FileList");
        }
    }
}