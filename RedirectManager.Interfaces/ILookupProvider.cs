using System;
using System.Collections.Generic;
namespace RedirectManager.Interfaces
{
	public interface ILookupProvider
	{
		bool IsReadOnly
		{
			get;
		}
		IRedirect LookupUrl(string requestUrlPath);
		IEnumerable<IRedirect> LookupItem(string id);
		bool Exists(string requestUrlPath);
		void CreateRedirect(string requestUrlPath, string targetItemId, bool autoGenerated);
		void DeleteRedirect(string requestUrlPath);
        void CreateQueryString(string requestUrlPath, string queryStringInput);
        void DeleteQueryString(string requestUrlPath);
        string ViewQueryString(string requestUrlPath);
	}
}
