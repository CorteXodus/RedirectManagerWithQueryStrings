using RedirectManager.Interfaces;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Reflection;
using System;
using System.Diagnostics;
using System.Linq;
namespace RedirectManager.Pipelines.HttpRequest
{
	public class Redirector : HttpRequestProcessor
	{
		private static ILookupProvider provider;
		public static ILookupProvider Provider
		{
			get
			{
				if (Redirector.provider == null)
				{
					Redirector.provider = (ILookupProvider)ReflectionUtil.CreateObjectFromConfig("dataproviders/redirectmanager");
					if (Redirector.provider == null)
					{
						throw new InvalidTypeException("Redirect Manager: failed to create LookupProvider from config");
					}
				}
				return Redirector.provider;
			}
		}

		public override void Process(HttpRequestArgs args)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			if (Redirector.Provider == null)
			{
				return;
			}
			if (Context.Site == null || Context.Database == null)
			{
				return;
			}
			if (Config.IgnoredSites.Contains(Context.Site.Name, StringComparer.OrdinalIgnoreCase))
			{
				return;
			}
			if (Redirector.IsIgnoredPageMode())
			{
				return;
			}
			if (Context.Item != null)
			{
				return;
			}
			IRedirect redirect = Redirector.Provider.LookupUrl(args.Context.Request.Path);
			if (redirect == null)
			{
				return;
			}
			if (!redirect.Enabled)
			{
				return;
			}
			if (Config.SiteContextChecking && redirect.Sites != null && !redirect.Sites.Contains(Context.Site.Name, StringComparer.OrdinalIgnoreCase))
			{
				return;
			}
			string targetUrl = redirect.GetTargetUrl(Context.Site.Name);
            string redirectQueryString = redirect.QueryString;
			if (!string.IsNullOrEmpty(targetUrl))
			{
				if (Config.LogProcessorStopwatch)
				{
					stopwatch.Stop();
					Log.Info(string.Format("Redirect Manager: httprequest processor total time is : {0}ms ({1} ticks)", ((double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0).ToString("F"), stopwatch.ElapsedTicks), this);
				}

                if (string.IsNullOrEmpty(redirectQueryString)) {
				    Redirector.Respond(args, redirect.ResponseStatusCode, redirect.ResponseStatusDescription, targetUrl);
                } else { 
                    Redirector.Respond(args, redirect.ResponseStatusCode, redirect.ResponseStatusDescription, targetUrl, redirectQueryString);
                }

			}
		}

		private static bool IsIgnoredPageMode()
		{
			return !Context.PageMode.IsNormal || (Context.PageMode.IsPageEditor || Context.PageMode.IsPreview || Context.PageMode.IsPageEditorEditing);
		}

		private static void Respond(HttpRequestArgs args, int responseStatusCode, string responseStatusDescription, string responseUrl)
		{
			args.Context.Response.Clear();
			args.Context.Response.StatusCode = responseStatusCode;
			args.Context.Response.StatusDescription = responseStatusDescription;
			args.Context.Response.RedirectLocation = responseUrl;
			args.Context.Response.Write(string.Format("<html><head>\n<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">\n<title>{0} Moved</title></head>\n", responseStatusCode));
			args.Context.Response.Write(string.Format("<body><h1>{0} Moved</h1>\nThe document has moved <a href=\"{1}\">here</a>.</body></html>", responseStatusCode, responseUrl));
			args.Context.Response.End();
		}

        private static void Respond(HttpRequestArgs args, int responseStatusCode, string responseStatusDescription, string responseUrl, string queryString)
        {
            args.Context.Response.Clear();
            args.Context.Response.StatusCode = responseStatusCode;
            args.Context.Response.StatusDescription = responseStatusDescription;
            args.Context.Response.RedirectLocation = responseUrl + "?" + queryString;
            args.Context.Response.Write(string.Format("<html><head>\n<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">\n<title>{0} Moved</title></head>\n", responseStatusCode));
            args.Context.Response.Write(string.Format("<body><h1>{0} Moved</h1>\nThe document has moved <a href=\"{1}\">here</a>.</body></html>", responseStatusCode, responseUrl));
            args.Context.Response.End();
        }

	}
}
