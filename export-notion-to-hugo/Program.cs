﻿using System.Text;
using Helpers;
using Models;
using Notion.Client;
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

        string outputDirectory = BuildOutputDirectory(parameters.TmpFolder, page);
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