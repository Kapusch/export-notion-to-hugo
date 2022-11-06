using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Helpers;
using Notion.Client;

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

    public async Task<PaginatedList<Page>> GetPagesFromDatabase(string databaseId)
    {
        var statusFilter = new StatusFilter("Status", equal: "Published");

        var queryParams = new DatabasesQueryParameters { Filter = statusFilter };
        return await client.Databases.QueryAsync(databaseId, queryParams);
    }

    public async Task<string> ExportPageToMarkdown(Page page)
    {
        // build frontmatter

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("---");

        //foreach (var property in page.Properties)
        //{
        // Parse the properties from expected parameters
        // Format the FrontMatterProperty in .TOML
        // stringBuilder.AppendLine($"{nameof(Paremeters.Title)}: \"{paremeters.Title}\"");
        //}

        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine("");

        string outputDirectory = "";

        // page content
        var paginatedBlocks = await client.Blocks.RetrieveChildrenAsync(page.Id);
        do
        {
            foreach (Block block in paginatedBlocks.Results)
            {
                await AppendBlockLineAsync(block, string.Empty, outputDirectory, stringBuilder);
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

        return stringBuilder.ToString();
    }

    #region MarkDown Helpers

    async Task AppendBlockLineAsync(Block block, string indent, string outputDirectory, StringBuilder stringBuilder)
    {
        switch (block)
        {
            case ParagraphBlock paragraphBlock:
                foreach (var text in paragraphBlock.Paragraph.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }

                stringBuilder.AppendLine(string.Empty);
                break;
            case HeadingOneBlock h1:
                stringBuilder.Append($"{indent}# ");
                foreach (var text in h1.Heading_1.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                stringBuilder.AppendLine(string.Empty);
                break;
            case HeadingTwoBlock h2:
                stringBuilder.Append($"{indent}## ");
                foreach (var text in h2.Heading_2.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                stringBuilder.AppendLine(string.Empty);
                break;
            case HeadingThreeeBlock h3:
                stringBuilder.Append($"{indent}### ");
                foreach (var text in h3.Heading_3.RichText)
                {
                    AppendRichText(text, stringBuilder);
                }
                stringBuilder.AppendLine(string.Empty);
                break;
            //case ImageBlock imageBlock:
            //    await AppendImageAsync(imageBlock, indent, outputDirectory, stringBuilder);
            //    stringBuilder.AppendLine(string.Empty);
            //    break;
            case CodeBlock codeBlock:
                AppendCode(codeBlock, indent, stringBuilder);
                stringBuilder.AppendLine(string.Empty);
                break;
            case BulletedListItemBlock bulletListItemBlock:
                AppendBulletListItem(bulletListItemBlock, indent, stringBuilder);
                break;
            case NumberedListItemBlock numberedListItemBlock:
                AppendNumberedListItem(numberedListItemBlock, indent, stringBuilder);
                break;
        }

        stringBuilder.AppendLine(string.Empty);

        if (block.HasChildren)
        {
            var pagination = await client.Blocks.RetrieveChildrenAsync(block.Id);
            do
            {
                foreach (Block childBlock in pagination.Results)
                {
                    await AppendBlockLineAsync(childBlock, $"    {indent}", outputDirectory, stringBuilder);
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

        if (richText.Annotations.IsStrikeThrough)
        {
            text = $"~{text}~";
        }

        stringBuilder.Append(text);
    }

    async Task AppendImageAsync(ImageBlock imageBlock, string indent, string outputDirectory, StringBuilder stringBuilder)
    {
        var url = string.Empty;
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
            var (fileName, _) = await DownloadImage(url, outputDirectory);
            stringBuilder.Append($"{indent}![](./{fileName})");
        }
    }

    async Task<(string, string)> DownloadImage(string url, string outputDirectory)
    {
        var uri = new Uri(url);
        using (var md5 = MD5.Create())
        {
            var input = Encoding.UTF8.GetBytes(uri.LocalPath);
            var fileName = $"{Convert.ToHexString(md5.ComputeHash(input))}{Path.GetExtension(uri.LocalPath)}";
            var filePath = $"{outputDirectory}/{fileName}";

            HttpClient client = new HttpClient();
            client.MaxResponseContentBufferSize = 100000000; // ~100 Mo

            using (var httpResponse = await client.GetAsync(uri).ConfigureAwait(false))
            {
                if(httpResponse.StatusCode is HttpStatusCode.OK)
                {
                    var downloadedImage = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    File.WriteAllBytes(filePath, downloadedImage);
                }
                else
                    return (null, null);
            }

            return (fileName, filePath);
        }
    }

    void AppendCode(CodeBlock codeBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"{indent}```{NotionCodeLanguageToMarkdownCodeLanguage(codeBlock.Code.Language)}");
        foreach (var richText in codeBlock.Code.RichText)
        {
            stringBuilder.Append(indent);
            AppendRichText(richText, stringBuilder);
            stringBuilder.AppendLine(string.Empty);
        }
        stringBuilder.AppendLine($"{indent}```");
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

    void AppendNumberedListItem(NumberedListItemBlock numberedListItemBlock, string indent, StringBuilder stringBuilder)
    {
        stringBuilder.Append($"{indent}1. ");
        foreach (var item in numberedListItemBlock.NumberedListItem.RichText)
        {
            AppendRichText(item, stringBuilder);
        }
    }

    #endregion
}