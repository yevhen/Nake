using System;

namespace Nake
{
    public class Interactive
    {
        public static string Ask(string question)
        {
            Console.WriteLine(question);
            return Console.ReadLine();
        }
    }
}
