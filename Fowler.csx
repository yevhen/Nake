#r "Tools\Nake\Meta.dll" 

using Nake;
using System;

[Task] public static void Default()
{
	Compile();
}

/// <summary> 
/// Take data configuration files and use them to generate the database structure and the code to access the database
/// </summary>
[Task] public static void CodeGen()
{
	Console.WriteLine("do code gen stuff");
}

/// <summary> 
/// Compile the application code
/// </summary>
[Task] public static void Compile()
{
	CodeGen();

	Console.WriteLine("do compile stuff");
}

/// <summary> 
/// Load test data into the database
/// </summary>
[Task] public static void DataLoad()
{
	CodeGen();

	Console.WriteLine("do data load stuff");
}

/// <summary> 
/// Run the tests
/// </summary>
[Task] public static void Test()
{
	Compile();
	DataLoad();

	Console.WriteLine("run tests");
}
