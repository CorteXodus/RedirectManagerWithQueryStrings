using System;
using System.Collections.Generic;
namespace RedirectManager.Interfaces
{
	public interface IRedirect
	{
		bool Enabled
		{
			get;
		}
        string QueryString
        {
            get;
        }
        string ResponseTargetId
		{
			get;
		}
		string RequestPath
		{
			get;
		}
		string ResponseStatusDescription
		{
			get;
		}
		IEnumerable<string> Sites
		{
			get;
		}
		int ResponseStatusCode
		{
			get;
		}
		string GetTargetUrl(string sitename);
	}
}
