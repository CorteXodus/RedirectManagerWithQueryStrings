using RedirectManager.Interfaces;
using RedirectManager.Pipelines.HttpRequest;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;
using System;

namespace RedirectManager.Shell.Framework.Pipelines
{
	[Serializable]
	public class DeleteRedirect
	{
		private ILookupProvider provider;
		public DeleteRedirect()
		{
			this.provider = Redirector.Provider;
		}
        
        //this thing is never even being called in our implementation...
        //public void CheckPermissions(ClientPipelineArgs args)
        //{
        //    Assert.ArgumentNotNull(args, "args");
        //    if (SheerResponse.CheckModified())
        //    {
        //        string str = args.Parameters["id"];

        //        // get the item URI
        //        ItemUri uri = Context.Item.Uri;

        //        // string representation
        //        string uriString = "sitecore://master/" + str + "?lang=en&ver=1";

        //        // parsing a string
        //        uri = ItemUri.Parse(uriString);

        //        // get an item by URI. Static Database.GetItem method can be used because URI includes database name.
        //        Item item = Database.GetItem(uri);

        //        if (item != null)
        //        {
        //            if (!item.Access.CanDelete())
        //            {
        //                SheerResponse.Alert(Translate.Text("You do not have permission to delete redirect item \"{0}\".", new object[]
        //                {
        //                    item.DisplayName
        //                }), new string[0]);
        //                args.AbortPipeline();
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            SheerResponse.Alert("Item not found.", new string[0]);
        //            args.AbortPipeline();
        //        }
        //    }
        //}
		public void Delete(ClientPipelineArgs args)
		{
            string requestPath = args.Parameters["requestPath"];
            string targetID = args.Parameters["targetId"];

			if (!args.IsPostBack)
			{
                Context.ClientPage.ClientResponse.Confirm(string.Format("Delete redirect \"{0}\"?", requestPath));
				args.WaitForPostBack();
				return;
			}
			if (!args.HasResult)
			{
				args.AbortPipeline();
				return;
			}
			if (args.Result == "yes")
			{
                Log.Audit(this, "Redirect Manager: deleting redirect which targets item ID: '{0}' with url redirect path : '{1}'", new string[]
			    {
                    targetID,
				    requestPath
			    });
                this.provider.DeleteRedirect(requestPath);
				Context.ClientPage.ClientResponse.Alert("Redirect Deleted");
                string eventName = string.Format("item:load(id={0})", targetID);
				Context.ClientPage.ClientResponse.Timer(eventName, 2);
				return;
			}
			Context.ClientPage.ClientResponse.Alert("Cancelled");
			args.AbortPipeline();
		}
	}
}
