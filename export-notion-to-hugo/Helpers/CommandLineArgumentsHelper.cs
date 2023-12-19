using System;
using Models;

namespace Helpers;

public static class CommandLineArgumentsHelper
{
    /// <summary>
    /// Parse command line arguments when running the program as below:
    /// dotnet run "NotionApiToken={YOUR_SECRET_TOKEN}" "DatabaseId={YOUR_DATABASE_ID}" "Status={YOUR_PAGE_STATUS}" "TmpFolder={YOUR_FOLDER_PATH}"
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns>Parsed parameters</returns>
    /// <exception cref="ArgumentException">Something wrong happen when parsing arguments</exception>
    public static Arguments ParseCommandLineArguments(string[] arguments)
    {
        Arguments parameters = new();
        var parameterNames = (parameters.GetType()).GetProperties().Select(q => q.Name);

        foreach (var argument in arguments)
        {
            if (!parameterNames.Any(parameterName => argument.StartsWith(parameterName)))
            {
                throw new ArgumentException($"Unexpected argument: {argument}");
            }
            else
            {
                var parameterName = parameterNames.First(parameterName => argument.StartsWith(parameterName));
                if (argument.IndexOf('=') == -1)
                {
                    throw new ArgumentException($"Cannot parse value for argument: {parameterName}");
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