using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Helpers;
using Models;
using Notion.Client;
using static System.Net.Mime.MediaTypeNames;

namespace Services;

public class NotionAPI
{
    NotionClient client;

    public NotionAPI(string authorizationToken)
    {
        client = NotionClientFactory.Create(new ClientOptions
        {
            AuthToken = authorizationToken
        });
    }

    public async Task<Page> GetPageById(string id) => await client.Pages.RetrieveAsync(id);

    public async Task<PaginatedList<Page>> GetPagesFromDatabase(string databaseId, string pageStatusFilter)
    {
        var statusFilter = new StatusFilter("Status", equal: pageStatusFilter);

        var queryParams = new DatabasesQueryParameters { Filter = statusFilter };
        return await client.Databases.QueryAsync(databaseId, queryParams);
    }

    public async Task<string> ExportPageToMarkdown(Page page, string outputDirectory, bool centerImages = true)
    {
        bool useDescriptionAsSummary = false;
        var stringBuilder = new StringBuilder();
        string languageCode = String.Empty;
        bool isPartOfSeries = false;
        string parsedPageSubcategory = String.Empty;

        #region Front Matter
        stringBuilder.AppendLine("---");

        var defaultProperties = Enum.GetNames<Properties>();
        foreach (var defaultProperty in defaultProperties)
        {
            if(page.Properties.Any(q => q.Key.Equals(defaultProperty)))
            {
                var pageProperty = page.Properties.First(q => q.Key.Equals(defaultProperty));

                string parsedValue = String.Empty;
                switch (Enum.Parse<Properties>(defaultProperty))
                {
                    case Properties.Topic:
                    case Properties.Title:
                    case Properties.Category:
                    case Properties.Subcategory:
                        if (NotionPropertiesHelper.TryParseAsPlainText(pageProperty.Value, out var plainText))
                        {
                            parsedValue = $"\"{plainText}\"";
                        }
                        break;
                    case Properties.Index:
                        if (NotionPropertiesHelper.TryParseAsPlainText(pageProperty.Value, out var parsedIndex))
                        {
                            parsedValue = $"\"{parsedIndex}\"";

                            if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Subcategory.ToString()], out parsedPageSubcategory))
                            {
                                if (!String.IsNullOrEmpty(parsedPageSubcategory))
                                {
                                    isPartOfSeries = true;
                                    stringBuilder.AppendLine($"series: [\"{parsedPageSubcategory}\"]");
                                }
                            }
                        }
                        break;
                    case Properties.Language:
                        if (NotionPropertiesHelper.TryParseAsPlainText(pageProperty.Value, out var parsedLanguageCode))
                        {
                            parsedValue = $"\"{parsedLanguageCode}\"";

                            switch (parsedLanguageCode)
                            {
                                case "French":
                                    languageCode = "fr";
                                    break;
                                case "English":
                                    languageCode = "en";
                                    break;
                            }
                        }
                        break;
                    case Properties.PublishDate:
                        if (NotionPropertiesHelper.TryParseAsDateTime(pageProperty.Value, out var dateTime))
                        {
                            // If the page has an index, it will be rendered in the published date time
                            // so that the pages are ordered correctly within a serie.
                            if (NotionPropertiesHelper.TryParseAsPlainText(page.Properties[Properties.Index.ToString()], out var parsedPageIndex))
                            {
                                dateTime = dateTime.AddSeconds(int.Parse(parsedPageIndex ?? "0"));
                            }

                            parsedValue = $"\"{dateTime.ToString("u")}\"";
                        }
                        break;
                    case Properties.Tags:
                        if (NotionPropertiesHelper.TryParseAsStringSet(pageProperty.Value, out var parsedTags))
                        {
                            var tags = parsedTags.Select(tag => $"\"{tag}\"").ToList();
                            parsedValue = $"[{string.Join(',', tags)}]";
                        }
                        break;
                    case Properties.Description:
                        if (NotionPropertiesHelper.TryParseAsPlainText(pageProperty.Value, out var plainTextDescription))
                        {
                            parsedValue = $"\"{plainTextDescription}\"";
                        }

                        useDescriptionAsSummary = !(String.IsNullOrWhiteSpace(plainTextDescription));
                        break;
                }

                stringBuilder.AppendLine($"{defaultProperty}: {parsedValue}");
            }
        }

        if(page.Cover != null)
        {
            string fileName = await DownloadCover(page.Cover, outputDirectory, languageCode);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            stringBuilder.AppendLine($"featuredImagePreview: '{fileNameWithoutExtension}'");
            stringBuilder.AppendLine("resources:");
            stringBuilder.AppendLine($"- name: '{fileNameWithoutExtension}'");
            stringBuilder.AppendLine($"  src: '{fileName}'");
        }

        stringBuilder.AppendLine("draft: false");

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine();

        if (useDescriptionAsSummary)
        {
            stringBuilder.AppendLine("<!--more-->");
            stringBuilder.AppendLine();
        }
        #endregion

        #region Main Content
        var paginatedBlocks = await client.Blocks.RetrieveChildrenAsync(page.Id);
        do
        {
            foreach (Block block in paginatedBlocks.Results)
            {
                await AppendBlockLineAsync(block, string.Empty, outputDirectory, stringBuilder, centerImages, languageCode, false);
            }

            if (!paginatedBlocks.HasMore)
            {
                break;
            }

            paginatedBlocks = await client.Blocks.RetrieveChildrenAsync(page.Id, new BlocksRetrieveChildrenParameters
            {
                StartCursor = paginatedBlocks.NextCursor,
            });
        } while (true);
        #endregion

        #region Series table of content

        if (isPartOfSeries)
        {
            stringBuilder.AppendLine("___");
            if(languageCode == "fr")
                stringBuilder.AppendLine("Plus d'articles dans la même série:");
            else
                stringBuilder.AppendLine("More articles in the series:");

            stringBuilder.AppendLine($"{{{{< series \"{parsedPageSubcategory}\" >}}}}");
        }
            #endregion

        return stringBuilder.ToString();
    }

    #region MarkDown Helpers

    async Task AppendBlockLineAsync(Block block, string indent, string outputDirectory, StringBuilder stringBuilder, bool centerImages, string languageCode, bool isTableColumnHeader)
    {
        switch (block)
        {
            case ParagraphBlock paragraphBlock:
                foreach (var text in paragraphBlock.Paragraph.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                stringBuilder.AppendLine();
                break;
            case HeadingOneBlock h1:
                stringBuilder.Append($"{indent}# ");
                foreach (var text in h1.Heading_1.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                break;
            case HeadingTwoBlock h2:
                stringBuilder.Append($"{indent}## ");
                foreach (var text in h2.Heading_2.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                break;
            case HeadingThreeeBlock h3:
                stringBuilder.Append($"{indent}### ");
                foreach (var text in h3.Heading_3.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                break;
            case ImageBlock imageBlock:
                await AppendImageAsync(imageBlock, indent, outputDirectory, stringBuilder, centerImages);
                break;
            case CodeBlock codeBlock:
                AppendCode(codeBlock, indent, stringBuilder, languageCode);
                break;
            case BulletedListItemBlock bulletListItemBlock:
                AppendBulletListItem(bulletListItemBlock, indent, stringBuilder);
                break;
            case NumberedListItemBlock numberedListItemBlock:
                AppendNumberedListItem(numberedListItemBlock, indent, stringBuilder);
                break;
            case ToDoBlock todoBlock:
                AppendTodoListItem(todoBlock, indent, stringBuilder);
                break;
            case CalloutBlock calloutBlock:
                AppendCallout(calloutBlock, indent, stringBuilder);
                break;
            case FileBlock fileBlock:
                await AppendFileAsync(fileBlock, indent, outputDirectory, stringBuilder);
                break;
            case DividerBlock dividerBlock:
                AppendHorizontalRule(stringBuilder);
                break;
            case TableBlock tableBlock:
                isTableColumnHeader = tableBlock.Table.HasColumnHeader;
                break;
            case TableRowBlock tableRowBlock:
                AppendTableRow(tableRowBlock, isTableColumnHeader, stringBuilder);
                break;
            default:
                string currentPageId = String.Empty;

                if(block.Parent is PageParent)
                {
                    currentPageId = ((PageParent)block.Parent).PageId;
                }

                Console.WriteLine($"Unsupported block at page: [{currentPageId}]");
                Console.WriteLine($"Block details: [{block}]");
                break;
        }

        stringBuilder.AppendLine();

        if (block.HasChildren)
        {
            var pagination = await client.Blocks.RetrieveChildrenAsync(block.Id);
            do
            {
                foreach (Block childBlock in pagination.Results)
                {
                    await AppendBlockLineAsync(childBlock, $"    {indent}", outputDirectory, stringBuilder, centerImages, languageCode, isTableColumnHeader);

                    // Only add column header to first TableRowBlock when parsing TableBlock children
                    if (isTableColumnHeader) isTableColumnHeader = false;
                }

                if (!pagination.HasMore)
                {
                    break;
                }

                pagination = await client.Blocks.RetrieveChildrenAsync(block.Id, new BlocksRetrieveChildrenParameters
                {
                    StartCursor = pagination.NextCursor,
                });
            } while (true);
        }
    }

    void AppendTableRow(TableRowBlock tableRowBlock, bool isTableColumnHeader, StringBuilder stringBuilder)
    {
        stringBuilder.Append("| ");

        foreach (var cell in tableRowBlock.TableRow.Cells)
        {
            foreach (var richText in cell)
            {
                AppendRichText(richText, stringBuilder);
            }

            stringBuilder.Append(" | ");
        }

        if (isTableColumnHeader)
        {
            stringBuilder.AppendLine();

            for (int i = 0; i < tableRowBlock.TableRow.Cells.Count(); i++)
            {
                stringBuilder.Append("| --- ");
            }

            stringBuilder.Append("|");
        }
    }

    void AppendHorizontalRule(StringBuilder stringBuilder)
    {
        stringBuilder.Append("___");
    }

    void AppendRichText(RichTextBase richText, StringBuilder stringBuilder)
    {
        var text = richText.PlainText;

        if (!string.IsNullOrEmpty(richText.Href))
        {
            text = $"[{text}]({richText.Href})";
        }

        if (richText.Annotations.IsCode)
        {
            text = $"`{text}`";
        }

        if (richText.Annotations.IsItalic && richText.Annotations.IsBold)
        {
            text = $"***{text}***";
        }
        else if (richText.Annotations.IsBold)
        {
            text = $"**{text}**";
        }
        else if (richText.Annotations.IsItalic)
        {
            text = $"*{text}*";
        }
        else if (richText.Annotations.IsUnderline)
        {
            text = $"<u>{text}</u>";
        }

        if (richText.Annotations.IsStrikeThrough)
        {
            text = $"~{text}~";
        }

        stringBuilder.Append(text);
    }

    async Task AppendImageAsync(ImageBlock imageBlock, string indent, string outputDirectory, StringBuilder stringBuilder, bool centerImages)
    {
        outputDirectory = Path.Combine(outputDirectory, "images");
        var url = string.Empty;

        StringBuilder captionText = new();
        var caption = string.Empty;

        switch (imageBlock.Image)
        {
            case ExternalFile externalFile:
                url = externalFile.External.Url;
                break;
            case UploadedFile uploadedFile:
                url = uploadedFile.File.Url;
                break;
        }

        if (!string.IsNullOrEmpty(url))
        {
            var (fileName, _) = await DownloadFile(url, outputDirectory);

            #region Image

            if (centerImages)
                stringBuilder.Append("<p align=\"center\">");
            else
                stringBuilder.Append("<p>");


            stringBuilder.AppendLine($"<img max-width=\"100%\" max-height=\"100%\" src=\"./images/{fileName}\" /></p>");

            #endregion

            #region Caption

            stringBuilder.Append("<figure>");

            foreach (var richText in imageBlock.Image.Caption)
            {
                AppendRichText(richText, captionText);
            }

            caption = captionText.ToString();

            if (!String.IsNullOrWhiteSpace(caption))
                stringBuilder.Append($"<figcaption class=\"image-caption\">{caption}</figcaption>");

            stringBuilder.Append("</figure>");

            #endregion
        }
    }

    async Task<(string, string)> DownloadFile(string url, string outputDirectory, string renamedFile = "")
    {
        var uri = new Uri(url);
        using (var md5 = MD5.Create())
        {
            var input = Encoding.UTF8.GetBytes(uri.LocalPath);
            var fileName =
                String.IsNullOrWhiteSpace(renamedFile)
                ? $"{Convert.ToHexString(md5.ComputeHash(input))}{Path.GetExtension(uri.LocalPath)}"
                : renamedFile; 
            var filePath = Path.Combine(outputDirectory, fileName);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            HttpClient client = new HttpClient();
            client.MaxResponseContentBufferSize = 100000000; // ~100 Mo

            using (var httpResponse = await client.GetAsync(uri).ConfigureAwait(false))
            {
                if(httpResponse.StatusCode is HttpStatusCode.OK)
                {
                    var downloadedFile = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    File.WriteAllBytes(filePath, downloadedFile);
                }
                else
                    return (null, null);
            }

            return (fileName, filePath);
        }
    }

    void AppendCode(CodeBlock codeBlock, string indent, StringBuilder stringBuilder, string languageCode)
    {
        string filenameText = "Filename";
        // Use Notion codeblock caption as filename
        if(codeBlock.Code.Caption != null && codeBlock.Code.Caption.Any())
        {
            switch (languageCode)
            {
                case "fr":
                    filenameText = "Nom du fichier ";
                    break;
                default:
                    break;
            }

            stringBuilder.Append($"<p align=\"center\" style=\"margin-bottom:-10px\"><strong>{filenameText}:</strong><code>");

            var fileName = codeBlock.Code.Caption.First();
            AppendRichText(fileName, stringBuilder);

            stringBuilder.AppendLine("</code></p>");
            stringBuilder.AppendLine();
        }

        stringBuilder.AppendLine($"{indent}```{NotionCodeLanguageToMarkdownCodeLanguage(codeBlock.Code.Language)}");
        foreach (var richText in codeBlock.Code.RichText)
        {
            stringBuilder.Append(indent);
            AppendRichText(richText, stringBuilder);
            stringBuilder.AppendLine();
        }
        stringBuilder.Append($"{indent}```");
    }

    string NotionCodeLanguageToMarkdownCodeLanguage(string language)
    {
        return language switch
        {
            "c#" => "csharp",
            _ => language,
        };
    }

    void AppendBulletListItem(BulletedListItemBlock bulletedListItemBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.Append($"{indent}* ");
        foreach (var item in bulletedListItemBlock.BulletedListItem.RichText)
        {
            AppendRichText(item, stringBuilder);
        }
    }

    void AppendTodoListItem(ToDoBlock todoBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.Append($"{indent}- [ ] ");
        foreach (var item in todoBlock.ToDo.RichText)
        {
            AppendRichText(item, stringBuilder);
        }
    }

    void AppendNumberedListItem(NumberedListItemBlock numberedListItemBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.Append($"{indent}1. ");
        foreach (var item in numberedListItemBlock.NumberedListItem.RichText)
        {
            AppendRichText(item, stringBuilder);
        }
    }

    /// <summary>
    /// Converting Notion Callout block into a custom Hugo shortcode syntax
    /// from the following theme: https://github.com/hugo-fixit
    /// </summary>
    /// <param name="calloutBlock"></param>
    /// <param name="indent"></param>
    /// <param name="stringBuilder"></param>
    void AppendCallout(CalloutBlock calloutBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine();

        StringBuilder calloutText = new();
        CalloutTypes calloutType = CalloutTypes.note;

        if (calloutBlock.Callout.Icon is EmojiObject)
        {
            string emoji = (calloutBlock.Callout.Icon as EmojiObject).Emoji;

            switch (emoji)
            {
                case "ℹ️":
                    calloutType = CalloutTypes.info;
                    break;
                case "💡":
                    calloutType = CalloutTypes.tip;
                    break;
                case "🐒":
                    calloutType = CalloutTypes.comment;
                    calloutText.Append(emoji + "‎ ‎ ");
                    break;
            }
        }

        // NOTE: We need to remove any default title with an empty character unicode
        stringBuilder.AppendLine($"{indent}{{{{< admonition type={calloutType.ToString()} title=\"‎ \" open=true >}}}}");

        foreach (var richText in calloutBlock.Callout.RichText)
        {
            AppendRichText(richText, calloutText);
        }
        stringBuilder.AppendLine($"{indent}{calloutText.ToString()}");
        stringBuilder.Append($"{indent}{{{{< /admonition >}}}}");
    }

    /// <summary>
    /// Converting Notion File block into a custom Hugo shortcode syntax
    /// from the following theme: https://github.com/hugo-fixit
    /// </summary>
    /// <param name="fileBlock"></param>
    /// <param name="indent"></param>
    /// <param name="outputDirectory"></param>
    /// <param name="stringBuilder"></param>
    async Task AppendFileAsync(FileBlock fileBlock, string indent, string outputDirectory, StringBuilder stringBuilder)
    {
        outputDirectory = Path.Combine(outputDirectory, "files");
        string url = String.Empty;

        switch (fileBlock.File)
        {
            case ExternalFile externalFile:
                url = externalFile.External.Url;
                break;
            case UploadedFile uploadedFile:
                url = uploadedFile.File.Url;
                break;
        }

        if (!string.IsNullOrEmpty(url))
        {
            int start = url.LastIndexOf('/') + 1;
            int end = url.IndexOf('?') - start;
            string fileName = url.Substring(start, end);

            await DownloadFile(url, outputDirectory, fileName);

            stringBuilder.Append(
                $"{indent}{{{{< link " +
                $"href=\"./files/{fileName}\" " +
                $"content=\"{fileName}\" " +
                $"title=\"Download {fileName}\" " +
                $"download=\"{fileName}\" " +
                $"card=true >}}}}");
        }
    }

    /// <summary>
    /// Download cover image
    /// </summary>
    /// <param name="coverImage"></param>
    /// <param name="outputDirectory"></param>
    /// <param name="languageCode"></param>
    /// <returns>the file name</returns>
    async Task<string> DownloadCover(FileObject coverImage, string outputDirectory, string languageCode)
    {
        string url = String.Empty;
        string fileName = String.Empty;

        switch (coverImage)
        {
            case ExternalFile externalFile:
                url = externalFile.External.Url;
                break;
            case UploadedFile uploadedFile:
                url = uploadedFile.File.Url;
                break;
        }

        if (!string.IsNullOrEmpty(url))
        {
            int start = url.LastIndexOf('/') + 1;
            int end = url.IndexOf('?') - start;
            string fileNameExtension = Path.GetExtension(url.Substring(start, end));

            fileName = "featured-image-preview" + $"-{languageCode}" + fileNameExtension;

            await DownloadFile(url, outputDirectory, fileName);
        }

        return fileName;
    }

    #endregion
}