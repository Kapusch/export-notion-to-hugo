# Export your Notion pages to Hugo

<mark>⚠️ *This program is optimized for Hugo websites using the [Fixit](https://github.com/hugo-fixit) theme.*</mark>

A .NET Console App to export your Notion pages as markdown and publish them to your Hugo static web site.

### Setup
Before getting started, you need to prepare some parameters:
- **Notion API Token**: [configure a new integration](https://www.notion.so/my-integrations) in order to generate a new API key ([learn more](https://developers.notion.com/docs/authorization))
- **Database ID**: get the [ID for the database](https://developers.notion.com/reference/database#all-databases) which you want to export the page
- **Page Status**: specify which pages to export with a given page status

### Notion page properties
The program is optimized to work with a minimum set of properties from your Notion pages (the property type is mentioned in parenthesis):
- (Topic) **Topic**: the topic of your post (used as the folder name),
- (Title) **Title**: the title of your post,
- (Select) **Category**: the main category of your post,
- (Select) **Subcategory**: the subcategory of your post,
- (Integer) **Index**: the index of your post in a specific serie (especially useful when several posts have the same PublishDate),
- (Date) **PublishDate**: the date when to publish your post,
- (Select) **Language**: the language of post ("French" or "English"),
- (Text) **Description**: summary of your post,
- (Multi-select) **Tags**: a list of tags associated to your post,
- (Status) **Status**: the status of your page.

### Run the program
Download the project and run the program with the following command:
```bash
dotnet run "NotionApiToken={YOUR_API_KEY}" "DatabaseId={YOUR_DATABASE_ID}" "Status={YOUR_PAGE_STATUS}"
```

**Note:** you can also specify the output folder with adding the parameter `"TmpFolder={YOUR_FOLDER_PATH}"` to your command.

# Inspired by
* [Notion SDK for .Net](https://github.com/notion-dotnet/notion-sdk-net)
* [Notion to Markdown](https://github.com/yucchiy/notion-to-markdown)
