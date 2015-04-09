using System;
namespace RedirectManager.Pipelines.PublishItem
{
	
    //no references to this enumerator, original purpose unclear
    //see the TODO in RedirectManager.Shell.Framework.Pipelines.AddRedirect perhaps?
    public enum ExistsResponse
	{
		NotFound,
		Found,
		FoundDifferent
	}
}
