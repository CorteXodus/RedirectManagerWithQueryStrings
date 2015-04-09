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
    public class DeleteQueryString
    {
        private ILookupProvider provider;
        public DeleteQueryString()
		{
			this.provider = Redirector.Provider;
		}
        public void Execute(ClientPipelineArgs args)
        {
            string requestPath = args.Parameters["requestPath"];
            string targetID = args.Parameters["targetId"];

			if (!args.IsPostBack)
			{
                Context.ClientPage.ClientResponse.Confirm(string.Format("Delete the QueryString for the redirect \"{0}\"?", requestPath));
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
                Log.Audit(this, "Redirect Manager: deleting QueryString for the redirect '{0}' which targets item ID: '{1}'", new string[]
			    {
				    requestPath,
                    targetID
			    });
                
                //do the dirty work
                this.provider.DeleteQueryString(requestPath);
				
                //let the user know it happened
                Context.ClientPage.ClientResponse.Alert("QueryString Deleted");
                
                //reload the content editor's view
                string eventName = string.Format("item:load(id={0})", targetID);
				Context.ClientPage.ClientResponse.Timer(eventName, 2);
				return;
			}
			Context.ClientPage.ClientResponse.Alert("Cancelled");
			args.AbortPipeline();
        }
    }
}
