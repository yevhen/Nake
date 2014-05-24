using Nake;
using Nake.Log;

using System;

public const string RootPath = "$NakeScriptDirectory$";
public const string OutputPath = RootPath + @"\Output";

/// Compile the application code
[Task] public static void Default()
{
	Compile();
}

/// Take data configuration files and use them to generate the database structure and the code to access the database
[Task] public static void CodeGen()
{
	Message("do code gen stuff");
}

/// Compile the application code
[Task] public static void Compile()
{
	CodeGen();

	Message("do compile stuff");
}

/// Load test data into the database
[Task] public static void DataLoad()
{
	CodeGen();

	Message("do data load stuff");
}

/// Run the tests
[Task] public static void Test()
{
	Compile();
	DataLoad();

	Message("run tests");
}
