using RedirectManager.Interfaces;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
namespace RedirectManager.Providers.Sql
{
	public class LookupProvider : ILookupProvider
	{
		private static readonly object Lock = new object();
		private static Dictionary<string, Redirect> pathDictionary;
		private static Dictionary<string, Redirect> PathDictionary
		{
			get
			{
				if (LookupProvider.pathDictionary == null)
				{
					object @lock;
					Monitor.Enter(@lock = LookupProvider.Lock);
					try
					{
						if (LookupProvider.pathDictionary == null)
						{
							Stopwatch stopwatch = new Stopwatch();
							stopwatch.Start();
							LookupProvider.pathDictionary = LookupProvider.LoadAll();
							stopwatch.Stop();
							Log.Info(string.Format("Redirect Manager SQL Provider populated Url lookup dictionary with {0} items in {1}ms", LookupProvider.pathDictionary.Count, stopwatch.ElapsedMilliseconds), typeof(LookupProvider));
						}
					}
					finally
					{
						Monitor.Exit(@lock);
					}
				}
				return LookupProvider.pathDictionary;
			}
		}

		private static string ConnectionString
		{
			get
			{
				ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings["RedirectManager"];
				Assert.IsNotNull(connectionStringSettings, "Redirect Manager SQL Provider: connection string entry not found for 'RedirectManager'");
				string connectionString = connectionStringSettings.ConnectionString;
				Assert.IsNotNullOrEmpty(connectionString, "Redirect Manager SQL Prrovider: connection string value empty");
				return connectionString;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public IRedirect LookupUrl(string requestPath)
		{
			if (!LookupProvider.PathDictionary.ContainsKey(requestPath))
			{
				return null;
			}
			return LookupProvider.PathDictionary[requestPath];
		}

		public IEnumerable<IRedirect> LookupItem(string id)
		{
			List<Redirect> list = new List<Redirect>();
			using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
			{
				sqlConnection.Open();
				using (SqlCommand sqlCommand = new SqlCommand())
				{
					sqlCommand.Parameters.Add(new SqlParameter("ResponseTargetId", DbType.Guid));
					sqlCommand.Parameters["ResponseTargetId"].Value = new Guid(id);
					sqlCommand.CommandText = "SELECT * FROM Redirects WHERE ResponseTargetId = @ResponseTargetId";
					sqlCommand.Connection = sqlConnection;
					SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					int ordinal = sqlDataReader.GetOrdinal("ResponseTargetId");
					int ordinal2 = sqlDataReader.GetOrdinal("RequestPath");
					int ordinal3 = sqlDataReader.GetOrdinal("ResponseStatusCode");
					int ordinal4 = sqlDataReader.GetOrdinal("Sites");
                    //QUERYSTRINGSTUFF
                    int ordinal5 = sqlDataReader.GetOrdinal("QueryString");
                    string queryString = string.Empty;
                    //QUERYSTRINGSTUFF

                    while (sqlDataReader.Read())
					{
                        //QUERYSTRINGSTUFF
                        if (!sqlDataReader.IsDBNull(ordinal5))
                        {
                            queryString = sqlDataReader.GetString(ordinal5);
                        }
                        else
                        {
                            queryString = string.Empty;
                        }
                        //QUERYSTRINGSTUFF
                        
                        //Redirect item = new Redirect(true, sqlDataReader.GetGuid(ordinal).ToString(), sqlDataReader.GetString(ordinal2), sqlDataReader.IsDBNull(ordinal4) ? null : sqlDataReader.GetString(ordinal4), sqlDataReader.GetInt32(ordinal3));
                        Redirect item = new Redirect(true, queryString, sqlDataReader.GetGuid(ordinal).ToString(), sqlDataReader.GetString(ordinal2), sqlDataReader.IsDBNull(ordinal4) ? null : sqlDataReader.GetString(ordinal4), sqlDataReader.GetInt32(ordinal3));
						list.Add(item);
					}
				}
			}
			return list.ToArray();
		}

		public bool Exists(string requestUrlPath)
		{
			bool result;
			using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
			{
				sqlConnection.Open();
				using (SqlCommand sqlCommand = new SqlCommand())
				{
					sqlCommand.Connection = sqlConnection;
					sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
					sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
					sqlCommand.CommandText = "SELECT COUNT(*) FROM Redirects WHERE RequestPath = @RequestPath";
					int num = (int)sqlCommand.ExecuteScalar();
					
                    if (num > 0)
					{
						result = true;
					}
					else
					{
						result = false;
					}
				}
			}
			return result;
		}

		public void CreateRedirect(string requestUrlPath, string responseTargetId, bool autoGenerated)
		{
			using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
			{
				sqlConnection.Open();
				using (SqlCommand sqlCommand = new SqlCommand())
				{
					sqlCommand.Parameters.Add(new SqlParameter("ResponseTargetId", SqlDbType.UniqueIdentifier));
					sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
					sqlCommand.Parameters.Add(new SqlParameter("ResponseStatusCode", SqlDbType.Int));
					sqlCommand.Parameters.Add(new SqlParameter("AutoGenerated", SqlDbType.Bit));
					sqlCommand.Parameters["ResponseTargetId"].Value = new Guid(responseTargetId);
					sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
					sqlCommand.Parameters["ResponseStatusCode"].Value = 301;
					sqlCommand.Parameters["AutoGenerated"].Value = autoGenerated;
					sqlCommand.CommandText = "INSERT INTO Redirects (ResponseTargetId, RequestPath, ResponseStatusCode, AutoGenerated) VALUES (@ResponseTargetId, @RequestPath, @ResponseStatusCode, @AutoGenerated)";
					sqlCommand.Connection = sqlConnection;
					
                    try
					{
						sqlCommand.ExecuteNonQuery();
					}
					catch (SqlException ex)
					{
						if (ex.Number == 8152)
						{
							throw new ApplicationException(string.Format("Redirect Manager: Url path '{0}' is longer ({1} chars) than the configured SQL data column will allow.\r\nIncrease column length or shorten item path.", requestUrlPath, requestUrlPath.Length), ex);
						}
						throw;
					}
				}
			}
		}

        public void DeleteRedirect(string requestUrlPath)
		{
			using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
			{
				sqlConnection.Open();
				using (SqlCommand sqlCommand = new SqlCommand())
				{
					sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
                    sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
					sqlCommand.CommandText = "DELETE FROM Redirects WHERE RequestPath = @RequestPath";
					sqlCommand.Connection = sqlConnection;
                    
                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new ApplicationException(string.Format("Redirect Manager: Deletion of the redirect '{0}' failed.\r\n", requestUrlPath), ex);
                    }
				}
			}
		}

        public void CreateQueryString(string requestUrlPath, string queryStringInput)
        {
            using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
                    sqlCommand.Parameters.Add(new SqlParameter("QueryString", SqlDbType.NVarChar));
                    sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
                    sqlCommand.Parameters["QueryString"].Value = queryStringInput;                    
                    sqlCommand.CommandText = "UPDATE Redirects SET Querystring=@QueryString WHERE RequestPath=@RequestPath";
                    sqlCommand.Connection = sqlConnection;

                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new ApplicationException(string.Format("Redirect Manager: QueryString addition to the redirect path '{0}' failed.\r\n", requestUrlPath), ex);
                    }
                }
            }

        }
        public void DeleteQueryString(string requestUrlPath)
        {
            using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
                    sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
                    sqlCommand.CommandText = "UPDATE Redirects SET Querystring=null WHERE RequestPath=@RequestPath";
                    sqlCommand.Connection = sqlConnection;

                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new ApplicationException(string.Format("Redirect Manager: Deletion of the redirect '{0}' failed.\r\n", requestUrlPath), ex);
                    }
                }
            }
        }

        public string ViewQueryString(string requestUrlPath)
        {
            string queryStringToView = null;

            using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Parameters.Add(new SqlParameter("RequestPath", SqlDbType.NVarChar));
                    sqlCommand.Parameters["RequestPath"].Value = requestUrlPath;
                    sqlCommand.CommandText = "SELECT QueryString FROM Redirects WHERE RequestPath=@RequestPath";
                    sqlCommand.Connection = sqlConnection;

                    try
                    {
                        queryStringToView = sqlCommand.ExecuteScalar().ToString();
                    }
                    catch (SqlException ex)
                    {
                        queryStringToView = (string.Format("Redirect Manager: Attempting to view the QueryString for path '{0}' failed.\r\n", requestUrlPath));
                        throw new ApplicationException(string.Format("Redirect Manager: Attempting to view the QueryString for path '{0}' failed.\r\n", requestUrlPath), ex);
                    }

                }
            }
            
            return queryStringToView;
        }

		//I don't think this is ever being called...
        public void ClearCache(object sender, EventArgs args)
		{
			LookupProvider.pathDictionary = null;
		}

		private static Dictionary<string, Redirect> LoadAll()
		{
			Dictionary<string, Redirect> dictionary = new Dictionary<string, Redirect>(StringComparer.OrdinalIgnoreCase);
			using (SqlConnection sqlConnection = new SqlConnection(LookupProvider.ConnectionString))
			{
				sqlConnection.Open();
				using (SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Redirects", sqlConnection))
				{
					SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					int ordinal = sqlDataReader.GetOrdinal("ResponseTargetId");
					int ordinal2 = sqlDataReader.GetOrdinal("RequestPath");
					int ordinal3 = sqlDataReader.GetOrdinal("ResponseStatusCode");
					int ordinal4 = sqlDataReader.GetOrdinal("Sites");
                    //QUERYSTRINGSTUFF
                    int ordinal5 = sqlDataReader.GetOrdinal("QueryString");
                    string queryString = string.Empty;
                    //QUERYSTRINGSTUFF

					while (sqlDataReader.Read())
                    {

                        //QUERYSTRINGSTUFF
                        queryString = string.Empty;
                        if (!sqlDataReader.IsDBNull(ordinal5))
                        {
                            queryString = sqlDataReader.GetString(ordinal5);
                        }
                        //QUERYSTRINGSTUFF

						//Redirect redirect = new Redirect(true, sqlDataReader.GetGuid(ordinal).ToString(), sqlDataReader.GetString(ordinal2), sqlDataReader.IsDBNull(ordinal4) ? null : sqlDataReader.GetString(ordinal4), sqlDataReader.GetInt32(ordinal3));
                        Redirect redirect = new Redirect(true, queryString, sqlDataReader.GetGuid(ordinal).ToString(), sqlDataReader.GetString(ordinal2), sqlDataReader.IsDBNull(ordinal4) ? null : sqlDataReader.GetString(ordinal4), sqlDataReader.GetInt32(ordinal3));
						if (!dictionary.ContainsKey(redirect.RequestPath))
						{
							dictionary[redirect.RequestPath] = redirect;
						}
						else
						{
							Log.Warn(string.Format("RedirectManager Sql Provider duplicate request path skipped : {0}", redirect.RequestPath), typeof(LookupProvider));
						}
					}
				}
			}
			return dictionary;
		}

        //public void CreateSqlTestData(string targetItemId, int count)
        //{
        //    Random rnd = new Random();
        //    Item item = Factory.GetDatabase("master").GetItem(targetItemId);
        //    if (item != null)
        //    {
        //        for (int i = 0; i < count; i++)
        //        {
        //            string text = this.RandomString(12, rnd);
        //            if (!this.Exists(text))
        //            {
        //                this.Create("/test-" + text, item.ID.ToString(), true);
        //            }
        //        }
        //    }
        //}

        //private string RandomString(int size, Random rnd)
        //{
        //    char[] array = new char[size];
        //    for (int i = 0; i < size; i++)
        //    {
        //        array[i] = "abcdefghijklmnopqrstuvwxyz"[rnd.Next("abcdefghijklmnopqrstuvwxyz".Length)];
        //    }
        //    return new string(array);
        //}
	}
}
