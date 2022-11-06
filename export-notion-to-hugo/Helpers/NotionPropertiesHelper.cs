using System;
using Notion.Client;

namespace Helpers
{
    public static class NotionPropertiesHelper
    {
        public static bool TryParseAsPlainText(PropertyValue value, out string text)
        {
            text = string.Empty;
            switch (value)
            {
                case RichTextPropertyValue richTextProperty:
                    foreach (var richText in richTextProperty.RichText)
                    {
                        text += richText.PlainText;
                    }
                    break;
                case TitlePropertyValue titleProperty:
                    foreach (var richText in titleProperty.Title)
                    {
                        text += richText.PlainText;
                    }
                    break;
                case SelectPropertyValue selectPropertyValue:
                    text = selectPropertyValue.Select.Name;
                    break;
                case NumberPropertyValue numberPropertyValue:
                    text = numberPropertyValue.Number?.ToString();
                    break;

                default:
                    return false;
            }

            return true;
        }

        public static bool TryParseAsDateTime(PropertyValue value, out DateTime dateTime)
        {
            dateTime = default;
            switch (value)
            {
                case DatePropertyValue dateProperty:
                    if (dateProperty.Date == null) return false;
                    if (!dateProperty.Date.Start.HasValue) return false;

                    dateTime = dateProperty.Date.Start.Value;

                    break;
                case CreatedTimePropertyValue createdTimeProperty:
                    if (!DateTime.TryParse(createdTimeProperty.CreatedTime, out dateTime))
                    {
                        return false;
                    }

                    break;
                case LastEditedTimePropertyValue lastEditedTimeProperty:
                    if (!DateTime.TryParse(lastEditedTimeProperty.LastEditedTime, out dateTime))
                    {
                        return false;
                    }

                    break;

                default:
                    if (!TryParseAsPlainText(value, out var plainText))
                    {
                        return false;
                    }

                    if (!DateTime.TryParse(plainText, out dateTime))
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }

        public static bool TryParseAsStringSet(PropertyValue value, out List<string> set)
        {
            set = new List<string>();
            switch (value)
            {
                case MultiSelectPropertyValue multiSelectProperty:
                    foreach (var selectValue in multiSelectProperty.MultiSelect)
                    {
                        set.Add(selectValue.Name);
                    }
                    break;
                default:
                    return false;
            }

            return true;
        }

        public static bool TryParseAsBoolean(PropertyValue value, out bool boolean)
        {
            boolean = false;
            switch (value)
            {
                case CheckboxPropertyValue checkboxProperty:
                    boolean = checkboxProperty.Checkbox;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}

