using System;
using System.IO;
using System.Linq;

namespace Nake
{
    public static class FileSetExtensions
    {
        public static FileSet Mirror(this FileSet source, string destination)
        {
            destination = Location.GetFullPath(destination);

            return source.Transform(item => Path.Combine(destination, item.RecursivePath, item.FullName));
        }

        public static FileSet Flatten(this FileSet source, string destination)
        {
            destination = Location.GetFullPath(destination);

            return source.Transform(item => Path.Combine(destination, item.FullName));
        }
        
        public static FileSet Transform(this FileSet source, Func<FileSet.Item, string> transform)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            return source.Transform(item => new FileSet.Item(transform(item)));
        }

        static FileSet Transform(this FileSet source, Func<FileSet.Item, FileSet.Item> transform)
        {
            var result = new FileSet();

            foreach (var item in source.Resolve())
            {
                result.Include(transform(item));
            }

            return result;
        }
    }
}
