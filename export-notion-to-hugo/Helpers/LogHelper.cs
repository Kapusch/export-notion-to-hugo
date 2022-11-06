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
        StringBuilder formattedMessage = new();
        int patternLength;

        while (message.Length > separator.Length)
        {
            int splitIndex = message.Substring(0, separator.Length).LastIndexOf(' ');

            if (splitIndex == -1) splitIndex = separator.Length;

            string nextLine = message.Substring(0, splitIndex);

            patternLength = (separator.Length - nextLine.Length) / 2;

            formattedMessage.AppendLine(
                String.Format("{0}{1}{2}",
                    separator.Substring(0, patternLength),
                    nextLine,
                    separator.Substring(patternLength + nextLine.Length)));

            message = message.Remove(0, splitIndex + 1);
        }

        patternLength = (separator.Length - message.Length) / 2;
        formattedMessage.Append(
            String.Format("{0}{1}{2}",
                separator.Substring(0, patternLength),
                message,
                separator.Substring(patternLength + message.Length)));

        return formattedMessage.ToString();
    }
}