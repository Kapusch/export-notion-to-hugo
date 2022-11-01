LogHelper.PrintStart();

// dotnet run "Username=user@email.com" "Password=1234" "DatabaseID=12345abcde" "Status=published"
Parameters parameters = new();
var parameterNames = (parameters.GetType()).GetProperties().Select(q => q.Name);

if (args.Length != 4)
{
    LogHelper.PrintError($"{args.Length} found arguments instead of 4");
    Exit();
}

foreach (var argument in args)
{
    if (!parameterNames.Any(parameterName => argument.StartsWith(parameterName)))
    {
        LogHelper.PrintError($"Unexpected argument: {argument}");
        Exit();
    }
    else
    {
        var parameterName = parameterNames.First(parameterName => argument.StartsWith(parameterName));
        if (argument.IndexOf('=') == -1)
        {
            LogHelper.PrintError($"Cannot parse value for argument: {parameterName}");
            Exit();
        }
        else
        {
            var argumentValue = argument.Substring(argument.IndexOf('=') + 1);
            (parameters.GetType())
                .GetProperty(parameterName)
                .SetValue(parameters, argumentValue);
        }
    }
}

LogHelper.PrintEnd();
Exit();

void Exit()
{
    Console.ReadKey(true);
    Environment.Exit(0);
}

class Parameters
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string DatabaseID { get; set; }
    public string Status { get; set; }
}