using System;

namespace TestEcho;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(args.Length);
        Array.ForEach(args, Console.WriteLine);
    }
}