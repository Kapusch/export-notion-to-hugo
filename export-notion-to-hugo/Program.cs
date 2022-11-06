using System.Text;
using Helpers;
using Models;
using Services;

LogHelper.PrintStart();

Parameters parameters = null;
NotionAPI notionAPI;

try
{
    parameters = CommandLineArgumentsHelper.ParseCommandLineArguments(args);
    notionAPI = new(parameters.NotionApiToken);

    var pagesRetrievedFromNotion = await notionAPI.GetPagesFromDatabase(parameters.DatabaseId);
    foreach (var page in pagesRetrievedFromNotion.Results)
    {
        string markdown = await notionAPI.ExportPageToMarkdown(page);

        string pageTitle = String.Empty;
        if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Title"], out var parsedTitle))
        {
            pageTitle = parsedTitle;
        }

        string outputDirectory = Path.Combine(parameters.TmpFolder, pageTitle ?? page.Id);
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string languageCode = String.Empty;
        if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Language"], out var parsedLanguage))
        {
            switch (parsedLanguage)
            {
                case "French":
                    languageCode = ".fr";
                    break;
                case "English":
                    languageCode = ".en";
                    break;
            }
        }

        using (var fileStream = File.OpenWrite($"{outputDirectory}/index{languageCode}.md"))
        {
            using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
            {
                await streamWriter.WriteAsync(markdown);
            }
        }
    }
}
catch (Exception ex)
{
    LogHelper.PrintError(ex.Message);
    Exit();
}

LogHelper.PrintEnd();
Exit();


void Exit()
{
    Console.ReadKey(true);
    Environment.Exit(0);
}