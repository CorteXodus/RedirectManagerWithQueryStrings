using RedirectManager.Interfaces;
using RedirectManager.Pipelines.HttpRequest;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace RedirectManager.Pipelines.GetContentEditorWarnings
{
	public class DisplayLinks
	{
		private enum DisplayModeEnum
		{
			Primary,
			Redirects,
			Aliases
		}

		private const string LimitResultsFormat = "<div style=\"Width:500px\" readonly=\"readonly\"><br />Limited to displaying the first {0} of {1} items</div>";
		private const string LinkFormat = "<div><a href=\"{0}\" target=\"_blank\"><input class=\"scEditorHeaderQuickInfoInput\" style=\"Width:500px\" readonly=\"readonly\" value=\"{0}\" /></a></div>";
		private const string RedirectFormat = "<div><span style=\"float:left; width:300px\">{5}<a href=\"{0}\" target=\"_blank\">{0}</a></span >&nbsp;<span style=\"width:50px\">{1}</span>&nbsp;<span style=\"width:60px\">[ {2} ]</span>&nbsp;<span style=\"width:60px\">[ {3} ]</span>&nbsp;<span>{4}</span></div>";
		
        public void Process(GetContentEditorWarningsArgs args)
		{
			Assert.IsNotNull(args, "Redirect Manager: displaylinks GetContentEditorWarningsArgs");
			SiteContext siteContext = DisplayLinks.ResolveSiteContext(args.Item);

			if (siteContext == null)
			{
				Log.Error("Redirect Manager: displaylinks could not resolve site", this);
				return;
			}

            //Build and display the primary link for the item as long as it's not a media item and it has a layout.
            if (DisplayLinks.DisplayModeEnabled(DisplayLinks.DisplayModeEnum.Primary) && !args.Item.Paths.IsMediaItem && args.Item.Visualization.Layout != null)
            {

                string itemUrl = UrlFromItemCompare(args.Item, siteContext);

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("<div><a href=\"{0}\" target=\"_blank\"><input class=\"scEditorHeaderQuickInfoInput\" style=\"Width:500px\" readonly=\"readonly\" value=\"{0}\" /></a></div>", itemUrl);

                if (stringBuilder.Length > 0)
				{
					args.Add("Primary Link", stringBuilder.ToString());
				}
			}

			//Build and display any redirects that exist for the item
            if (DisplayLinks.DisplayModeEnabled(DisplayLinks.DisplayModeEnum.Redirects))
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				int num = 1;
				IEnumerable<IRedirect> redirectItems = DisplayLinks.GetRedirectItems(args.Item);
				
                if (redirectItems != null)
				{
					foreach (IRedirect current in redirectItems)
					{
						if (!this.IsSamePath(args, siteContext, current.RequestPath))
						{
							if (Config.SiteContextChecking)
							{
								string empty = string.Empty;
                                string txtQueryViewControls = string.Empty;
                                string txtQueryControls = string.Empty;
								string txtRedirectInfo = string.Format("<a href=\"#\" onclick='javascript:return scForm.invoke(\"{0}\")' title=\"Delete redirect\">[ Delete ]</a>",
                                    string.Format("redirect:delete(targetId={0}, requestPath={1})", args.Item.ID, HttpUtility.HtmlEncode(current.RequestPath)));
                                
                                //logic here for querystring relevant information to be displayed based on the existence or lack of a querystring for a given redirect
                                if (string.IsNullOrEmpty(current.QueryString))
                                {
                                    //I don't need the ItemID for the querystring gen, but for perms checking. I DO need the path of this particular redirect in order to gen
                                    //an invocation which will provide the ability to add a querystring to that particular redirect.
                                    txtQueryControls = string.Format("<a href=\"#\" onclick='javascript:return scForm.invoke(\"{0}\")' title=\"Add QueryString\">[ Add QueryString ]</a>",
                                        string.Format("querystring:add(targetId={0}, requestPath={1})", args.Item.ID, HttpUtility.HtmlEncode(current.RequestPath)));
                                }
                                else
                                {
                                    //gen invocation for viewing the querystring to append to the deletion invocation presentation
                                    txtQueryViewControls = string.Format("<a href=\"#\" onclick='javascript:return scForm.invoke(\"{0}\")' title=\"View QueryString\">[ View QueryString ]</a>",
                                        string.Format("querystring:view(targetId={0}, requestPath={1})", current.QueryString, HttpUtility.HtmlEncode(current.RequestPath)));
                                    
                                    //gen invocation to delete the querystring, adds the above invocation to complete the presentation of the querystring remove/view for the stringbuilder below
                                    txtQueryControls = string.Format("<a href=\"#\" onclick='javascript:return scForm.invoke(\"{0}\")' title=\"Delete QueryString\">[ Delete QueryString ]</a>",
                                        string.Format("querystring:delete(targetId={0}, queryString={1}, requestPath={2})", args.Item.ID, current.QueryString, HttpUtility.HtmlEncode(current.RequestPath))) + " " + txtQueryViewControls;
                                }

                                string txtLanguage = string.Empty;
								
                                //TODO:
                                //this thing is hard-coded for English, which is entirely unhelpful for anyone else. Maybe look into this later...
                                if (LinkManager.LanguageEmbedding != LanguageEmbedding.Never)
								{
									txtLanguage = "<span style=\"color:#999999\">/en</span>";
								}

                                stringBuilder2.AppendFormat("<div><span style=\"font-weight: bold; width:300px\">{4}<a href=\"{0}\" target=\"_blank\">{0}</a><div style=\"float:right\"></span><span>{1}</span>&nbsp;<span>[ {2} ]</span>&nbsp;<span>[ {3} ]</span>&nbsp;<span>{5}</span>&nbsp;<span>{4}</span>&nbsp;<span>{6}</span>&nbsp;</div></div><hr style=\"border: 0; height: 0; margin-right: 20px; border-top: 1px solid rgba(0, 0, 0, 0.1);\">", new object[]
								{
									current.RequestPath,
									txtRedirectInfo,
									current.Enabled ? "<span style=\"color:green\">Enabled</span>" : "<span style=\"text-decoration:line-through; color:red\">Enabled</span>",
									current.ResponseStatusCode,
									empty,
                                    txtQueryControls,
									txtLanguage
								});
							}
							else
							{
								stringBuilder2.AppendFormat("<div><a href=\"{0}\" target=\"_blank\"><input class=\"scEditorHeaderQuickInfoInput\" style=\"Width:500px\" readonly=\"readonly\" value=\"{0}\" /></a></div>", current.RequestPath);
							}
						}

						if (num >= Config.DisplayMaxLinks)
						{
							stringBuilder2.AppendFormat("<div style=\"Width:500px\" readonly=\"readonly\"><br />Limited to displaying the first {0} of {1} items</div>", Config.DisplayMaxLinks, redirectItems.Count<IRedirect>());
							break;
						}
						num++;
					}
				}

                if (stringBuilder2.Length > 0)
				{
					args.Add("Urls that redirect here", stringBuilder2.ToString());
				}
			}

			if (DisplayLinks.DisplayModeEnabled(DisplayLinks.DisplayModeEnum.Aliases))
			{
				StringBuilder stringBuilder3 = new StringBuilder();
				IEnumerable<Item> aliasItems = DisplayLinks.GetAliasItems(args.Item);
				int num2 = 1;
				if (aliasItems != null)
				{
					foreach (Item current2 in aliasItems)
					{
						string arg = current2.Paths.Path.ToLowerInvariant().Replace("/sitecore/system/aliases".ToLowerInvariant(), string.Empty);
						stringBuilder3.AppendFormat("<div><a href=\"{0}\" target=\"_blank\"><input class=\"scEditorHeaderQuickInfoInput\" style=\"Width:500px\" readonly=\"readonly\" value=\"{0}\" /></a></div>", arg);
						if (num2 >= Config.DisplayMaxLinks)
						{
							stringBuilder3.AppendFormat("<div style=\"Width:500px\" readonly=\"readonly\"><br />Limited to displaying the first {0} of {1} items</div>", Config.DisplayMaxLinks, aliasItems.Count<Item>());
							break;
						}
						num2++;
					}
				}
				if (stringBuilder3.Length > 0)
				{
					string str = Settings.AliasesActive ? string.Empty : " (aliases not active)";
					args.Add("Aliases" + str, stringBuilder3.ToString());
				}
			}
		}

        private bool IsSamePath(GetContentEditorWarningsArgs args, SiteContext siteContext, string requestPath)
        {
            //UrlOptions urlOptions = DisplayLinks.GetItemUrlOptions(siteContext);
            //urlOptions.LanguageEmbedding = LanguageEmbedding.Never;
            //string itemUrl = LinkManager.GetItemUrl(args.Item, urlOptions);
            string itemUrl = UrlFromItemCompare(args.Item, siteContext);
            Uri uri = new Uri(itemUrl);
            return uri.LocalPath.Equals(requestPath, StringComparison.OrdinalIgnoreCase) || (uri.LocalPath + ".aspx").Equals(requestPath, StringComparison.OrdinalIgnoreCase);
        }

        //compares the context item to the master db's home item in order to properly resolve URLs
        private string UrlFromItemCompare(Item item, SiteContext siteContext)
        {
            //get the home item since we'll need to do a comparison on it.
            string homeItemPath = Sitecore.Context.Site.StartPath.ToString();
            Sitecore.Data.Database master = Sitecore.Configuration.Factory.GetDatabase("master");
            Item startItem = master.GetItem(homeItemPath);

            //get some ID's for comparison
            ID homeItemID = startItem.ID;
            ID argItemID = item.ID;

            //gen an empty string to build into for the url
            string itemURL = "";

            if (argItemID != homeItemID) //if the item is anything but Home use the normal site options including the use of full server URL and .aspx extension
            {
                itemURL = LinkManager.GetItemUrl(item, DisplayLinks.GetItemUrlOptions(siteContext));
            }
            else //if the item is the home item, then omit the badly appended .aspx extension which otherwise appears
            {
                itemURL = LinkManager.GetItemUrl(item, DisplayLinks.GetHomeUrlOptions(siteContext));
            }

            return itemURL;
        }

        private static bool DisplayModeEnabled(DisplayLinks.DisplayModeEnum mode)
        {
            return Config.DisplayLinkTypes.Contains(mode.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        //TODO:
        //Consolidate the two below methods into a single method which sets Aspx extension based on 'ishomeitem" boolean
        private static UrlOptions GetItemUrlOptions(SiteContext siteContext)
        {
            UrlOptions urlOptions = UrlOptions.DefaultOptions;
            urlOptions.AlwaysIncludeServerUrl = true;
            urlOptions.Site = siteContext;
            return urlOptions;
        }

        private static UrlOptions GetHomeUrlOptions(SiteContext siteContext)
        {
            UrlOptions urlOptions = UrlOptions.DefaultOptions;
            urlOptions.AlwaysIncludeServerUrl = true;
            urlOptions.AddAspxExtension = false;
            urlOptions.Site = siteContext;
            return urlOptions;
        }

		private static SiteContext ResolveSiteContext(Item item)
		{
			return (
				from site in SiteManager.GetSites()
				where !Config.IgnoredSites.Contains(site.Name)
				select SiteContextFactory.GetSiteContext(site.Name) into sc
				where sc != null && item.Paths.Path.StartsWith(sc.StartPath, StringComparison.OrdinalIgnoreCase)
				select sc).FirstOrDefault<SiteContext>();
		}

		private static IEnumerable<Item> GetAliasItems(Item item)
		{
			LinkDatabase linkDatabase = Factory.GetLinkDatabase();
			IEnumerable<ItemLink> source = 
				from link in linkDatabase.GetReferrers(item)
				where link.SourceDatabaseName.Equals(item.Database.Name)
				where link.SourceFieldID.Equals(RedirectManager.FieldIDs.AliasLinkedItem)
				where link.GetSourceItem().TemplateID.Equals(Sitecore.TemplateIDs.Alias)
				select link;
			return 
				from link in source
				select link.GetSourceItem();
		}

		private static IEnumerable<IRedirect> GetRedirectItems(Item item)
		{
			ILookupProvider provider = Redirector.Provider;
			return provider.LookupItem(item.ID.ToString());
		}
	}
}
