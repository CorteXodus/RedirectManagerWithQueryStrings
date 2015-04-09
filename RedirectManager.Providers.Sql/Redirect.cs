using RedirectManager.Interfaces;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using System;
using System.Collections.Generic;
namespace RedirectManager.Providers.Sql
{
	public class Redirect : IRedirect
	{
		public bool Enabled
		{
			get;
			private set;
		}

        public string QueryString
        {
            get;
            private set;
        }

		public string ResponseTargetId
		{
			get;
			private set;
		}

		public string ResponseStatusDescription
		{
			get;
			private set;
		}

		public int ResponseStatusCode
		{
			get;
			private set;
		}

		public string RequestPath
		{
			get;
			private set;
		}

		public IEnumerable<string> Sites
		{
			get;
			private set;
		}

		public Redirect(bool enabled, string queryString, string responseTargetId, string requestPath, string sites, int responseStatusCode)
		{
			this.Enabled = enabled;
            this.QueryString = queryString;
			this.RequestPath = requestPath;
			this.ResponseStatusCode = responseStatusCode;
			this.ResponseTargetId = responseTargetId;
			this.Sites = ((sites != null) ? sites.Split(new char[]
			{
				'|'
			}) : null);
		}

		public string GetTargetUrl(string sitename)
		{
			Item targetItem = this.GetTargetItem();
			if (targetItem != null)
			{
				UrlOptions defaultUrlOptions = LinkManager.GetDefaultUrlOptions();
				defaultUrlOptions.AlwaysIncludeServerUrl = true;
				return LinkManager.GetItemUrl(targetItem);
			}
			return null;
		}

		private Item GetTargetItem()
		{
			Database database = Context.Database;
			Assert.IsNotNull(database, "Context database");
			return database.Items[this.ResponseTargetId];
		}
	}
}
