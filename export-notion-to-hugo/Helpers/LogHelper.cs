using System;
using System.Text;
namespace Helpers;

public static class LogHelper
{
    const string SERAPATOR_LINE_1 = "..::..::..::..::..::..::..::..::..::..::..";
    const string SERAPATOR_LINE_2 = "..........................................";

    public static void PrintStart()
    {
        Console.WriteLine(SERAPATOR_LINE_1);
        Console.WriteLine(FormatMessage("EXPORT NOTION TO HUGO", SERAPATOR_LINE_2));
        Console.WriteLine(FormatMessage(". . . . . START . . . . .", SERAPATOR_LINE_2));
        Console.WriteLine(SERAPATOR_LINE_1);
    }

    public static void PrintError(string errorMessage)
    {
        Console.WriteLine();
        Console.WriteLine(FormatMessage(". . . . . ERROR . . . . .", SERAPATOR_LINE_2));
        Console.WriteLine(FormatMessage(errorMessage, SERAPATOR_LINE_2));
        Console.WriteLine(SERAPATOR_LINE_2);
    }

    public static void PrintMessage(string message)
    {
        Console.WriteLine();
        Console.WriteLine(FormatMessage(message, SERAPATOR_LINE_2));
    }

    public static void PrintEnd()
    {
        Console.WriteLine();
        Console.WriteLine(SERAPATOR_LINE_1);
        Console.WriteLine(FormatMessage(". . . . . SUCCESS . . . . .", SERAPATOR_LINE_2));
        Console.WriteLine(SERAPATOR_LINE_1);
    }

    static string FormatMessage(string message, string separator = SERAPATOR_LINE_1)
    {
        if(message.Length > separator.Length)
        {
            message = message.Substring(0, separator.Length - 5);
            message += "[...]";
        }

        StringBuilder formattedMessage = new StringBuilder(separator);

        int patternLength = (separator.Length - message.Length) / 2;

        for (int i = 0; i < message.Length; i++)
        {
            formattedMessage[i + patternLength] = message[i];
        }

        return formattedMessage.ToString();
    }
}