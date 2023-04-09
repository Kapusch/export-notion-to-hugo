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

    List<Page> test = new List<Page>();
    // NOTE: Uncomment for debugging a specific page export
    //test.Add(await notionAPI.GetPageById("753688cef4674473b8380b7f56a19827"));

    var pagesRetrievedFromNotion = await notionAPI.GetPagesFromDatabase(parameters.DatabaseId, parameters.Status);
    foreach (var page in test.Count == 0 ? pagesRetrievedFromNotion.Results : test)
    {
        string outputDirectory = BuildOutputDirectory(parameters.TmpFolder, page);
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string markdown = await notionAPI.ExportPageToMarkdown(page, outputDirectory);

        string languageCode = String.Empty;
        if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Language.ToString()], out var parsedLanguage))
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

    AddIndexFiles(parameters.TmpFolder);
}
catch (Exception ex)
{
#if DEBUG
    Console.WriteLine(ex);
#else
    LogHelper.PrintError(ex.Message);
#endif
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
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Index.ToString()], out var parsedPageIndex))
    {
        if (!String.IsNullOrEmpty(parsedPageIndex))
        {
            pageIndex = parsedPageIndex + "-";
        }
    }

    // The topic of a page is used as the post URL
    string pageTopic = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Topic.ToString()], out var parsedTopic))
    {
        pageTopic = parsedTopic;
    }

    // The category of a page will classify the post as a tuto or a tip
    string pageCategory = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Category.ToString()], out var parsedCategory))
    {
        pageCategory = parsedCategory;
    }

    string pageSubcategory = String.Empty;
    if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Subcategory.ToString()], out var parsedSubcategory))
    {
        pageSubcategory = parsedSubcategory;
    }

    return
        Path.Combine(baseOutput,
        "posts",
        pageCategory ?? "Misc",
        pageSubcategory,
        pageIndex + (pageTopic ?? page.Id));
}

/// <summary>
/// Create the index file for each category and subcategories
/// </summary>
/// <param name="baseOutput"></param>
void AddIndexFiles(string baseOutput)
{
    string outputDirectory = Path.Combine(baseOutput, "posts");

    foreach (var categoryFolder in Directory.GetDirectories(outputDirectory))
    {
        string category =
            Path.GetFileName(categoryFolder)
            ?? throw new ApplicationException("Not a valid directory name");

        AddIndexFile(categoryFolder, category);

        foreach (var subcategoryFolder in Directory.GetDirectories(categoryFolder))
        {
            string subcategory =
                Path.GetFileName(subcategoryFolder)
                ?? throw new ApplicationException("Not a valid directory name");

            // If the page has a sub-category, it will be used as a sub-path to classify
            // the post as within a serie
            if (Char.IsDigit(subcategory[0]))
            {
                AddIndexFile(subcategoryFolder, subcategory);
            }
        }
    }
}

/// <summary>
/// Add an index file in order to list folder content
/// </summary>
/// <param name="outputDirectory"></param>
/// <param name="title"></param>
void AddIndexFile(string outputDirectory, string title)
{
    var stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("---");
    stringBuilder.AppendLine($"Title: {title}");
    stringBuilder.AppendLine("draft: false");
    stringBuilder.AppendLine("---");
    stringBuilder.AppendLine(String.Empty);


    using (var fileStream = File.OpenWrite($"{outputDirectory}/_index.md"))
    {
        using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
        {
            streamWriter.Write(stringBuilder.ToString());
        }
    }
}