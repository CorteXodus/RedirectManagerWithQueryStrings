using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using System;

namespace RedirectManager.Shell.Framework.Commands
{
	[Serializable]
	public class AddRedirect : Command
	{
		public override void Execute(CommandContext context)
		{
			Assert.ArgumentNotNull(context, "context");
			if (context.Items.Length == 1 && context.Items[0].Access.CanWrite())
			{
				base.Start("uiAddRedirect", context.Items[0]);
			}
		}
		public override CommandState QueryState(CommandContext context)
		{
			if (context.Items.Length == 1)
			{
				Item item = context.Items[0];
				if (item.Paths.IsContentItem && item.Access.CanWrite())
				{
					return base.QueryState(context);
				}
			}
			return CommandState.Disabled;
		}
	}
}
