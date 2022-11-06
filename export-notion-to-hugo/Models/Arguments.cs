using System;
namespace Models;

public class Arguments
{
    public string TmpFolder { get; set; }
        = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            DateTime.Now.ToString("yyyyMMdd-HHmm-")+ "notion-export");
    public string NotionApiToken { get; set; }
    public string DatabaseId { get; set; }
    public string Status { get; set; }
}