using System;
namespace Models;

public class Arguments
{
    /// <summary>
    /// Output folder
    /// </summary>
    public string TmpFolder { get; set; }
        = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            DateTime.Now.ToString("yyyyMMdd-HHmm-")+ "notion-export");

    /// <summary>
    /// Notion API key
    /// </summary>
    public string NotionApiToken { get; set; }

    /// <summary>
    /// The ID of the targeted database
    /// </summary>
    public string? DatabaseId { get; set; }

    /// <summary>
    /// The targeted page status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// A list of page IDs separated by semicolon
    /// </summary>
    public string? PageIds { get; set; }
}