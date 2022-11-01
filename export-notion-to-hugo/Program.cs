using Helpers;
using Models;

Parameters parameters;

LogHelper.PrintStart();

try
{
    CommandLineArgumentsHelper.ParseCommandLineArguments(args);
}
catch (Exception ex)
{
    Exit();
}

LogHelper.PrintEnd();
Exit();

void Exit()
{
    Console.ReadKey(true);
    Environment.Exit(0);
}