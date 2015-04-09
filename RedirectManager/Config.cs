using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using System;
namespace RedirectManager
{
	public static class Config
	{
		public static string[] IgnoredSites
		{
			get
			{
				return StringUtil.Split(Settings.GetSetting("RedirectManager.IgnoredSites", string.Empty), char.Parse("|"), false);
			}
		}
		public static string[] DisplayLinkTypes
		{
			get
			{
				return StringUtil.Split(Settings.GetSetting("RedirectManager.DisplayLinkTypes", string.Empty), char.Parse("|"), false);
			}
		}
		public static string UrlPathValidationExpression
		{
			get
			{
				string setting = Settings.GetSetting("RedirectManager.UrlPathValidation");
				Assert.IsNotNullOrEmpty(setting, "Redirect Manager: UrlPathValidation setting is required");
				return setting;
			}
		}
        public static string QueryStringValidationExpression
        {
            get
            {
                string setting = Settings.GetSetting("RedirectManager.QueryStringValidation");
                Assert.IsNotNullOrEmpty(setting, "Redirect Manager: QueryStringValidation setting is required");
                return setting;
            }
        }
		public static bool CheckDuplicateUrlOnCreate
		{
			get
			{
				return Settings.GetBoolSetting("RedirectManager.CheckDuplicates", true);
			}
		}
		public static bool SiteContextChecking
		{
			get
			{
				return Settings.GetBoolSetting("RedirectManager.SiteContextChecking", false);
			}
		}
		public static bool LogProcessorStopwatch
		{
			get
			{
				return Settings.GetBoolSetting("RedirectManager.LogProcessorStopwatch", false);
			}
		}
		public static int DisplayMaxLinks
		{
			get
			{
				return Settings.GetIntSetting("RedirectManager.DisplayLinkMaximum", 30);
			}
		}
		public static T Convert<T>(string str)
		{
			return (T)((object)Enum.Parse(typeof(T), str, true));
		}
	}
}
