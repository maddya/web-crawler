using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable
{
	public class AzureStorage : iStorageAccount
	{
		private static ErrorListEntity Errors { get; set; }

		public void AddLinkToResults(string URL, string title, DateTime date)
		{
			CloudTable table = CloudConfiguration.GetSiteDataTable();
			string[] words = title.Split(null); // split title on whitespace
			foreach (string s in words)
			{
				string keyword = s.ToLower();
				URLEntity linkEntry = new URLEntity(URL, keyword, title, date);

				// Create the TableOperation object that inserts the customer entity.
				TableOperation insertOperation = TableOperation.Insert(linkEntry);

				try
				{
					// Execute the insert operation.
					table.Execute(insertOperation);
				} catch(Exception ex)
				{
					Debug.Print(ex.Message);
				}
			}
		}

		public void AddLinkForParsing(string URL)
		{
			CloudQueue crawlQueue = CloudConfiguration.GetCrawlingQueue();
			CloudQueueMessage message = new CloudQueueMessage(URL);
			crawlQueue.AddMessage(message);
		}

		public void AddSitemap(string URL)
		{
			CloudQueue loadQueue = CloudConfiguration.GetLoadingQueue();
			CloudQueueMessage message = new CloudQueueMessage(URL);
			loadQueue.AddMessage(message);
		}

	}
}
