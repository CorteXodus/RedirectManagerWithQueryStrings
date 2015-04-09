using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using System;

namespace RedirectManager.Shell.Framework.Commands
{
    [Serializable]
    public class DeleteQueryString : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if (context.Items.Length == 1 && context.Items[0].Access.CanWrite())
            {
                context.Parameters.Add("id", context.Items[0].ID.ToString());
                Context.ClientPage.Start("uiDeleteQueryString", context.Parameters);
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
