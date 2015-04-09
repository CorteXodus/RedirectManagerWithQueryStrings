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
    class ViewQueryString
    {
        private ILookupProvider provider;
        public ViewQueryString()
		{
			this.provider = Redirector.Provider;
		}
        public void Execute(ClientPipelineArgs args)
        {
            string requestPath = args.Parameters["requestPath"];
            string targetID = args.Parameters["targetId"];

            string queryStringToView = this.provider.ViewQueryString(requestPath).ToString();

            Context.ClientPage.ClientResponse.Alert(queryStringToView);

            return;
        }
    }
}
