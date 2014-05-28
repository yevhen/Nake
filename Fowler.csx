using Nake;
using Nake.Log;

/// Compile the application code
[Task] void Default()
{
    Compile();
}

/// Take data configuration files and use them to generate the database structure and the code to access the database
[Task] void CodeGen()
{
    Message("do code gen stuff");
}

/// Compile the application code
[Task] void Compile()
{
    CodeGen();

    Message("do compile stuff");
}

/// Load test data into the database
[Task] void DataLoad()
{
    CodeGen();

    Message("do data load stuff");
}

/// Run the tests
[Task] void Test()
{
    Compile();
    DataLoad();

    Message("run tests");
}