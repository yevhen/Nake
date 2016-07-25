using static Nake.Log;

/// Calls Greeting() task
[Task] void Default()
{
	Greeting();
}

/// Prints traditional greeting
[Task] void Greeting()
{
	Info("Hello from Nake!");
}