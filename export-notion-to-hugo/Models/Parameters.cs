using System;
namespace Models;

public class Parameters
{
    public string TmpFolder { get; set; }
        = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string NotionApiToken { get; set; }
    public string DatabaseId { get; set; }
    public string Status { get; set; }
}

