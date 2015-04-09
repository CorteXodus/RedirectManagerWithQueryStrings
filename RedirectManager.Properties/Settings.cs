using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
namespace RedirectManager.Properties
{
	[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0"), CompilerGenerated]
	internal sealed class Settings : ApplicationSettingsBase
	{
		private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());
		public static Settings Default
		{
			get
			{
				return Settings.defaultInstance;
			}
		}
		[ApplicationScopedSetting, DefaultSettingValue("Data Source=localhost;Initial Catalog=Sitecore_RedirectSuite;Integrated Security=True"), SpecialSetting(SpecialSetting.ConnectionString), DebuggerNonUserCode]
		public string Sitecore_RedirectSuiteConnectionString
		{
			get
			{
				return (string)this["Sitecore_RedirectSuiteConnectionString"];
			}
		}
	}
}
