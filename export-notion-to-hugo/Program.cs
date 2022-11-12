using System.Text;
using Helpers;
using Models;
using Notion.Client;
using Services;

LogHelper.PrintStart();

Arguments parameters = null;
NotionAPI notionAPI;

try
{
    parameters = CommandLineArgumentsHelper.ParseCommandLineArguments(args);
    notionAPI = new(parameters.NotionApiToken);

    List<Page> test = null;
    // NOTE: Uncomment for debugging a specific page export
    //test.Add(await notionAPI.GetPageById("24e6a22c6ee541ef9e4e0708f7d7da18"));

    var pagesRetrievedFromNotion = await notionAPI.GetPagesFromDatabase(parameters.DatabaseId, parameters.Status);
    foreach (var page in test ?? pagesRetrievedFromNotion.Results)
    {
        string outputDirectory = BuildOutputDirectory(parameters.TmpFolder, page);
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string markdown = await notionAPI.ExportPageToMarkdown(page, outputDirectory);

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

string BuildOutputDirectory(string baseOutput, Page page)
{
    string pageIndex = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Index"], out var parsedPageIndex))
    {
        pageIndex = parsedPageIndex + "-";
    }

    string pageTitle = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Title"], out var parsedTitle))
    {
        pageTitle = parsedTitle;
    }

    string pageCategory = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Category"], out var parsedCategory))
    {
        pageCategory = parsedCategory;
    }

    string pageSubcategory = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties["Subcategory"], out var parsedSubcategory))
    {
        pageSubcategory = parsedSubcategory;
    }

    return
        Path.Combine(baseOutput,
        "posts",
        pageCategory ?? "Misc",
        pageSubcategory,
        pageIndex + (pageTitle ?? page.Id));
}