using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;
using WorkerRole1;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
		/**
         * stuff for the dashboard:
         * State of each worker role web crawler (loading, crawling, idle)
         * Machine counters (CPU Utilization%, RAM available)
         * #URLs crawled
         * Last 10 URLs crawled
         * Size of queue (#urls left in pipeline to be crawled)
         * Size of index (Table storage with our crawled data)
         * Any errors and their URLs 
         */


		private CloudQueue LoadQueue { get; set; }
        private CloudQueue CrawlQueue { get; set; }
        private CloudQueue StopQueue { get; set; }
        private CloudTable SiteDataTable { get; set; }
		private CloudTable AdminStatusTable { get; set; }
		private CloudTable ErrorTable { get; set; }
		private static Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>();

		static HttpClient client = new HttpClient();

		[WebMethod]
		public string StartCrawler()
		{
			StopQueue = CloudConfiguration.GetStopQueue();
			CloudQueueMessage stopMessage = StopQueue.GetMessage();
			while (stopMessage != null)
			{
				StopQueue.DeleteMessage(stopMessage);
				stopMessage = StopQueue.GetMessage();
			}

			LoadQueue = CloudConfiguration.GetLoadingQueue();

			//Add message
			CloudQueueMessage startMessage = new CloudQueueMessage("http://www.cnn.com/robots.txt http://www.bleacherreport.com/robots.txt");
			LoadQueue.AddMessage(startMessage);

			return LoadQueue.Name + " " + startMessage.AsString;
		}

		[WebMethod]
		public string StopCrawler()
		{
			StopQueue = CloudConfiguration.GetStopQueue();
			CloudQueueMessage stopSignal = new CloudQueueMessage("stop");
			StopQueue.AddMessage(stopSignal);
			return StopQueue.Name + " " + stopSignal.AsString;
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public string GetSearchResults(string query)
		{
			SiteDataTable = CloudConfiguration.GetSiteDataTable();
			query = query.Trim().ToLower();
			if (cache.ContainsKey(query))
			{
				return new JavaScriptSerializer().Serialize(cache[query]);
			}
			else
			{
				var keywords = query.Split(null)
					.Select(x => Base64.Base64Encode(x));

				var results = new List<URLEntity>();

				foreach (string keyword in keywords)
				{
					TableQuery<URLEntity> rangeQuery = new TableQuery<URLEntity>()
					.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, keyword));

					var data = SiteDataTable.ExecuteQuery(rangeQuery);
					results.AddRange(data);
				}

				var siteMatches = results.GroupBy(x => x.URL)
						.Select(group => new Tuple<string, int, string>(group.Key, group.Count(), group.First().Title))
						.OrderByDescending(tuple => tuple.Item2);

				var links = siteMatches.Select(x => x.Item1 + "$" + x.Item3).ToList<string>();
				cache.Add(query, links);
				return new JavaScriptSerializer().Serialize(links);
			}
		}

		[WebMethod]
		public void ClearEverything()
		{
			SiteDataTable.DeleteIfExists();
			LoadQueue.DeleteIfExists();
			CrawlQueue.DeleteIfExists();
			StopQueue.DeleteIfExists();
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public string GetStatus()
		{
			AdminStatusTable = CloudConfiguration.GetAdminStatusTable();
			var status = AdminStatusTable.ExecuteQuery(new TableQuery<AdminStatus>()).ToList();
			return new JavaScriptSerializer().Serialize(status);
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public string GetErrors()
		{
			ErrorTable = CloudConfiguration.GetErrorTable();
			var errors = AdminStatusTable.ExecuteQuery(new TableQuery<ErrorListEntity>()).ToList();
			return new JavaScriptSerializer().Serialize(errors);
		}

	}
}