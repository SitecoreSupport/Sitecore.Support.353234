namespace Sitecore.Support.Shell.Applications.ContentManager.ReturnFieldEditorValues
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Reflection;
    using Sitecore.Shell.Applications.ContentEditor;
    using Sitecore.Shell.Applications.ContentManager;
    using Sitecore.Shell.Applications.ContentManager.ReturnFieldEditorValues;
    using System.Web.UI;

    public class SetValues
    {
        public void Process(ReturnFieldEditorValuesArgs args)
        {
            foreach (FieldInfo info in args.FieldInfo.Values)
            {
                Control control = Context.ClientPage.FindSubControl(info.ID);
                if (control != null)
                {
                    string str;
                    if (control is IContentField)
                    {
                        string[] values = new string[] { (control as IContentField).GetValue() };
                        str = StringUtil.GetString(values);
                    }
                    else
                    {
                        str = StringUtil.GetString(ReflectionUtil.GetProperty(control, "Value"));
                    }
                    if (str != "__#!$No value$!#__")
                    {
                        string str2 = info.Type.ToLowerInvariant();
                        if ((str2 == "rich text") || (str2 == "html"))
                        {
                            char[] trimChars = new char[] { ' ' };
                            str = str.TrimEnd(trimChars);
                        }
                        foreach (FieldDescriptor descriptor in args.Options.Fields)
                        {
                            if (!(descriptor.FieldID == info.FieldID))
                            {
                                continue;
                            }
                            ItemUri uri = new ItemUri(info.ItemID, info.Language, info.Version, Factory.GetDatabase(descriptor.ItemUri.DatabaseName));
                            if (descriptor.ItemUri == uri)
                            {
                                // added logic that encode "&" symbol
                                str = str.Replace("&", "&amp;");
                                descriptor.Value = str;
                            }
                        }
                    }
                }
            }
        }
    }
}