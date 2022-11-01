using System;
using Models;

namespace Helpers;

public static class CommandLineArgumentsHelper
{
    /// <summary>
    /// Parse command line arguments when running the program as below:
    /// dotnet run "Username=user@email.com" "Password=1234" "DatabaseID=12345abcde" "Status=published"
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns>Parsed parameters</returns>
    /// <exception cref="ArgumentException">Something wrong happen when parsing arguments</exception>
    public static Parameters ParseCommandLineArguments(string[] arguments)
    {
        Parameters parameters = new();
        var parameterNames = (parameters.GetType()).GetProperties().Select(q => q.Name);

        if (arguments.Length != 4)
        {
            LogHelper.PrintError($"{arguments.Length} found arguments instead of 4");
            throw new ArgumentException();
        }

        foreach (var argument in arguments)
        {
            if (!parameterNames.Any(parameterName => argument.StartsWith(parameterName)))
            {
                LogHelper.PrintError($"Unexpected argument: {argument}");
                throw new ArgumentException();
            }
            else
            {
                var parameterName = parameterNames.First(parameterName => argument.StartsWith(parameterName));
                if (argument.IndexOf('=') == -1)
                {
                    LogHelper.PrintError($"Cannot parse value for argument: {parameterName}");
                    throw new ArgumentException();
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

        return parameters;
    }
}