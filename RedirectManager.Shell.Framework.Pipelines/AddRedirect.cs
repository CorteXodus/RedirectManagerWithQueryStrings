using RedirectManager.Interfaces;
using RedirectManager.Pipelines.HttpRequest;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;
using System;

namespace RedirectManager.Shell.Framework.Pipelines
{
	[Serializable]
	public class AddRedirect
	{
		private ILookupProvider provider;
		public AddRedirect()
		{
			this.provider = Redirector.Provider;
		}
		public void CheckPermissions(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			if (SheerResponse.CheckModified())
			{
				string path = args.Parameters["id"];
				Item item = AddRedirect.PipelineContextDatabase(args).GetItem(path);
				if (item != null)
				{
					if (!item.Access.CanWrite())
					{
						SheerResponse.Alert(Translate.Text("You do not have permission to redirect item \"{0}\".", new object[]
						{
							item.DisplayName
						}), new string[0]);
						args.AbortPipeline();
						return;
					}
				}
				else
				{
					SheerResponse.Alert("Item not found.", new string[0]);
					args.AbortPipeline();
				}
			}
		}
		public void GetName(ClientPipelineArgs args)
		{
			if (!args.IsPostBack)
			{
				Context.ClientPage.ClientResponse.Input("Enter the url path to be redirected to this item e.g. /promo", "/", Config.UrlPathValidationExpression, "'$Input' is not a valid url path.", 9999);
                args.WaitForPostBack();
				return;
			}
			if (args.HasResult)
			{
				args.Parameters["urlPathInput"] = args.Result;
				return;
			}
			args.AbortPipeline();
		}
		public void CheckDuplicate(ClientPipelineArgs args)
		{
			if (Config.CheckDuplicateUrlOnCreate && this.provider.Exists(args.Parameters["urlPathInput"]))
			{
                //TODO:
                //There needs to be an additional check in place here. While the redirect itself may exist, the item which it references may have been deleted.
                //*NORMALLY* an item's deletion would also eliminate the corresponding redirect(s) for that item. However, it's been seen repeatedly that if a
                //user creates a redirect for an item and then immediately deletes the item itself, the redirect sometimes remains. This "orphaned" redirect
                //prevents the usage of a given redirect path since it technically already exists, and since the corresponding item is gone, the redirect can
                //only be freed "manually" by removing it from the SQL DB directly.
                
                //Ideal contingency handling:
                //Alert on existing redirect, offer to switch the redirect to the new targetId when original targetId can be found.
                //Alert on existing, offer to delete the redirect when targetId cannot be found and then set to the new targetId.

				SheerResponse.Alert(string.Format("Error: An existing redirect exists for the supplied url path '{0}'", args.Parameters["urlPathInput"]), new string[0]);
				args.AbortPipeline();
			}
		}
		public void Execute(ClientPipelineArgs args)
		{
			Assert.ArgumentNotNull(args, "args");
			string itemID = args.Parameters["id"];
			string urlPathInput = args.Parameters["urlPathInput"];
			Log.Audit(this, "Redirect Manager: adding redirect for item ID: '{0}' at url path : '{1}'", new string[]
			{
                itemID,
				urlPathInput
			});
            this.provider.CreateRedirect(urlPathInput, itemID, false);
			Context.ClientPage.ClientResponse.Alert("Redirect Added");
            string eventName = string.Format("item:load(id={0})", itemID);
			Context.ClientPage.ClientResponse.Timer(eventName, 2);
		}
		private static Database PipelineContextDatabase(ClientPipelineArgs args)
		{
			Database database = Factory.GetDatabase(args.Parameters["database"]);
			Assert.IsNotNull(database, args.Parameters["database"]);
			return database;
		}
	}
}
