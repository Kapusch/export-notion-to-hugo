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

        string tmp = await notionAPI.ExportPageToMarkdown(page);
        break;


        //var outputDirectory = BuildOutputDirectory(title, language);
        //if (!Directory.Exists(outputDirectory))
        //{
        //    Directory.CreateDirectory(outputDirectory);
        //}

        //using (var fileStream = File.OpenWrite($"{outputDirectory}/index.markdown"))
        //{
        //    using (var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false)))
        //    {
        //        await streamWriter.WriteAsync(stringBuilder.ToString());
        //    }
        //}
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