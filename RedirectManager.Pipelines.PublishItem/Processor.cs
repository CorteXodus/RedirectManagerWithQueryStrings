using RedirectManager.Pipelines.HttpRequest;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Publishing.Pipelines.PublishItem;
using Sitecore.Sites;
using Sitecore.Web;
using System;
using System.Linq;
namespace RedirectManager.Pipelines.PublishItem
{
	public class Processor : PublishItemProcessor
	{
		public override void Process(PublishItemContext context)
		{

            Database sourceDB = context.PublishOptions.SourceDatabase;

            Item item = context.PublishOptions.TargetDatabase.GetItem(context.ItemId);

            if (item == null)
			{
				return;
			}
			
            foreach (SiteInfo current in Settings.Sites)
			{
			    if (!Config.IgnoredSites.Contains(current.Name, StringComparer.OrdinalIgnoreCase) && item.Paths.ContentPath.StartsWith(current.StartItem, StringComparison.OrdinalIgnoreCase))
				{
                    SiteContext siteContext = SiteContextFactory.GetSiteContext(current.Name);
                    string homeItemPath = siteContext.StartPath.ToString();
                    Item startItem = sourceDB.GetItem(homeItemPath);

                    ID homeItemID = startItem.ID;
                    ID publishedItemID = item.ID;

                    string text = "";

                    if (publishedItemID != homeItemID)
                    {
                        text = LinkManager.GetItemUrl(context.PublishOptions.TargetDatabase.GetItem(context.ItemId), GetItemUrlOptions(siteContext));
                    }
                    else
                    {
                        text = LinkManager.GetItemUrl(context.PublishOptions.TargetDatabase.GetItem(context.ItemId), GetHomeUrlOptions(siteContext));
                    }

                    if (text.StartsWith("://"))
				    {
						text = "http" + text;
					}

                    string localPath;

                    try
					{
						Uri uri = new Uri(text);
						localPath = uri.LocalPath;
					}
					catch (UriFormatException innerException)
					{
						throw new ApplicationException(string.Format("Redirect Manager failed parsing item url generated by linkmanager. Url : {0} ItemId : {1}, ", text, context.ItemId), innerException);
					}

					if (!Redirector.Provider.Exists(localPath))
					{
						Redirector.Provider.CreateRedirect(localPath, context.ItemId.ToString(), true);
					}
					else
					{
						Redirector.Provider.DeleteRedirect(localPath);
						Redirector.Provider.CreateRedirect(localPath, context.ItemId.ToString(), true);
					}
				}
			}
		}
        
        private static UrlOptions GetItemUrlOptions(SiteContext siteContext)
        {
            UrlOptions urlOptions = LinkManager.GetDefaultUrlOptions();
            urlOptions.Site = siteContext;
            urlOptions.SiteResolving = true;
            urlOptions.AlwaysIncludeServerUrl = true;
            urlOptions.LanguageEmbedding = LanguageEmbedding.Never;
            return urlOptions;
        }

        private static UrlOptions GetHomeUrlOptions(SiteContext siteContext)
        {
            UrlOptions urlOptions = LinkManager.GetDefaultUrlOptions();
            urlOptions.Site = siteContext;
            urlOptions.SiteResolving = true;
            urlOptions.AlwaysIncludeServerUrl = true;
            urlOptions.AddAspxExtension = false;
            urlOptions.LanguageEmbedding = LanguageEmbedding.Never;
            return urlOptions;
        }
	}
}