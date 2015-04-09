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
using System.Web;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace RedirectManager.Shell.Framework.Pipelines
{
    [Serializable]
    public class AddQueryString
    {
        private ILookupProvider provider;
        
        public AddQueryString()
		{
			this.provider = Redirector.Provider;
		}

        public void CheckPermissions(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SheerResponse.CheckModified())
            {
                string itemID = args.Parameters["id"];
                Item item = AddQueryString.PipelineContextDatabase(args).GetItem(itemID);
                if (item != null)
                {
                    if (!item.Access.CanWrite())
                    {
                        SheerResponse.Alert(Translate.Text("You do not have permission to add a querystring to item \"{0}\".", new object[]
						{
							item.DisplayName
						}), new string[0]);
                        args.AbortPipeline();
                        return;
                    }
                }
                else
                {
                    SheerResponse.Alert("Item not found!", new string[0]);
                    args.AbortPipeline();
                }
            }
        }

        public void GetQueryString(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                //ask the user for a querystring, and run a Javascript regex validator (stored in the redirectmanager.config file) against that input.
                Context.ClientPage.ClientResponse.Input("Enter the QueryString to apply to this redirect", "?", Config.QueryStringValidationExpression, "'$Input' is not a valid QueryString.", 9999);
                args.WaitForPostBack();
                return;
            }

            if (args.HasResult)
            {
                //Whatever we got from the user theoretically passed the Javascript regex validator in order to get this far.
                //Regardless, we're now going to look more closely at the input.
                string inputToParse = args.Result.ToString();
                
                //this may be what gets inserted into the DB assuming all goes well below
                string queryString = null;

                //if we somehow managed to get something empty from the input, we're not gonna use it.
                if (inputToParse.Length > 0)
                {
                    //check if the input even HAS a '?'
                    //Find the '?'! this check is redundant since the javascript input regex should ensure that the first character is
                    //a '?', but I'm feeling better doint it safe rather than being sorry. We do this even though we're forcing the user to
                    //ensure a '?' exists at the front of their input because we like to make sure they know who is boss here.
                    int iqs = inputToParse.IndexOf('?');
                    if (iqs == 0)
                    {

                        //now let us lop that '?' off the front and then make sure there're no more of them hiding in the input!
                        queryString = inputToParse.Substring(iqs + 1);

                        //if there was more than one '?' in the user's input, we're not gonna take it!
                        if (!(queryString.Split('?').Length > 1))
                        {
                            //Let's do some more extensive validation here by harvesting the querystring's parameters and values
                            //and then hitting them with a regex to make sure they look solid.
                            NameValueCollection qscoll = HttpUtility.ParseQueryString(queryString);
                            Regex qsValidatorRegex = new Regex(@"([\w]+(=[\w-]*)?)+");
                            bool isValidQueryString = true;

                            //let's iterate through the key/val pairs we pulled from the querystring input
                            for (int i = 0; i <= qscoll.Count - 1; i++)
                            {
                                string qsKey = qscoll.GetKey(i);
                                string qsVal = qscoll.Get(qsKey);

                                //we will allow the values to be empty but not the keys
                                if (!string.IsNullOrEmpty(qsKey))
                                { 
                                    //see if the key/val pair looks ok
                                    Match queryMatch = qsValidatorRegex.Match(qsKey + "=" + qsVal);
                                    if (!queryMatch.Success)
                                    {
                                        isValidQueryString = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    isValidQueryString = false;
                                    break;
                                }
                            }

                            if (isValidQueryString)
                            {
                                args.Parameters["queryStringInput"] = queryString;
                                return;
                            }
                            else
                            {
                                SheerResponse.Alert(Translate.Text("There is a problem with the querystring you've entered. Please confirm syntax and try again."));
                                args.AbortPipeline();
                            }
                        }
                        else
                        {
                            SheerResponse.Alert(Translate.Text("A querystring may only contain a single question mark."));
                            args.AbortPipeline();
                        }
                    }
                    else
                    {
                        SheerResponse.Alert(Translate.Text("A querystring must begin with a question mark"));
                        args.AbortPipeline();
                    }
                }
                else
                {
                    SheerResponse.Alert(Translate.Text("Somehow, the input came in as empty! No funny business allowed!"));
                    args.AbortPipeline();
                }

            }//endif arg hasresult

            //if the args had no result, bail out of this pipeline
            args.AbortPipeline();

        }//end GetQueryString

        public void Execute(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            string itemID = args.Parameters["id"];
            string redirectUrlPath = args.Parameters["requestPath"];
            string queryStringInput = args.Parameters["queryStringInput"];
            string itemUrlPath = args.Parameters["itemUrlPath"];

            if (!string.IsNullOrEmpty(queryStringInput))
            {
                this.provider.CreateQueryString(redirectUrlPath, queryStringInput);
                Context.ClientPage.ClientResponse.Alert("QueryString Added");
                string eventName = string.Format("item:load(id={0})", itemID);
                Context.ClientPage.ClientResponse.Timer(eventName, 2);

                Log.Audit(this, "Redirect Manager: adding QueryString to the redirect '{0}' for item ID: '{1}' at url path : '{2}'", new string[]
                {
                    redirectUrlPath,
                    itemID,
                    itemUrlPath
                });
            }
            else
            {
                SheerResponse.Alert(Translate.Text("The field must not be empty!"));
                args.AbortPipeline();
            }

        }

        private static Database PipelineContextDatabase(ClientPipelineArgs args)
        {
            Database database = Factory.GetDatabase(args.Parameters["database"]);
            Assert.IsNotNull(database, args.Parameters["database"]);
            return database;
        }

    }
}
