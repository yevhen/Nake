#r "./Tools/Nake/Nake.Meta.dll"
#r "./Tools/Nake/Nake.Utility.dll"

using Nake;
using static Nake.Log;

/// Compile the application code
[Step] void Default()
{
    Compile();
}

/// Take data configuration files and use them to generate the database structure and the code to access the database
[Step] void CodeGen()
{
    Message("do code gen stuff");
}

/// Compile the application code
[Step] void Compile()
{
    CodeGen();

    Message("do compile stuff");
}

/// Load test data into the database
[Step] void DataLoad()
{
    CodeGen();

    Message("do data load stuff");
}

/// Run the tests
[Step] void Test()
{
    Compile();
    DataLoad();

    Message("run tests");
}