using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

		public static string State { get; private set; }
		private static PerformanceCounter CPUCount;
		private static PerformanceCounter MemCount;

		private static iStorageAccount Storage { get; set; }
		private static CloudQueue LoadQueue { get; set; }
		private static CloudQueue CrawlQueue { get; set; }
		private static CloudQueue StopQueue { get; set; }
		private static CloudTable SiteDataTable { get; set; }
		private static CloudTable AdminStatusTable { get; set; }
		private static AdminStatus Status { get; set; }
		private static WebCrawler Crawler;

		public override void Run()
		{
			Storage = new AzureStorage();

			LoadQueue = CloudConfiguration.GetLoadingQueue();
			CrawlQueue = CloudConfiguration.GetCrawlingQueue();
			StopQueue = CloudConfiguration.GetStopQueue();
			SiteDataTable = CloudConfiguration.GetSiteDataTable();
			AdminStatusTable = CloudConfiguration.GetAdminStatusTable();

			State = "Idle";

			CPUCount = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			MemCount = new PerformanceCounter("Memory", "Available MBytes");

			Status = new AdminStatus(State, (int)CPUCount.NextValue(), (int)MemCount.NextValue());

			string[] robots = { "http://www.cnn.com/robots.txt", "http://www.bleacherreport.com/robots.txt" };
			Crawler = new WebCrawler(robots, Storage);

			Thread.Sleep(10000);

			CloudQueueMessage stopMessage = StopQueue.GetMessage();

			string url = "";

			while (true)
			{
				while (stopMessage == null)
				{
					// Get the next message
					CloudQueueMessage loadMessage = LoadQueue.GetMessage();

					if (loadMessage != null)
					{
						State = "Loading";
						url = loadMessage.AsString;
						if (url.Contains("robots.txt"))
						{
							foreach(string link in robots)
							{
								Crawler.ProcessURL(link);
							}
							LoadQueue.DeleteMessage(loadMessage);
						}
						else
						{
							Crawler.ProcessURL(url);
						}
					}
					else if (State.Equals("Loading") || State.Equals("Crawling"))
					{
						CloudQueueMessage crawlMessage = CrawlQueue.GetMessage();
						// dequeue crawl message
						if (crawlMessage != null)
						{
							State = "Crawling";
							url = crawlMessage.AsString;
							Crawler.ProcessURL(url);
							CrawlQueue.DeleteMessage(crawlMessage);
						}
					}
					stopMessage = StopQueue.GetMessage();
					UpdateDashboard(url);
				}
				State = "Idle";
			}
		}

		private void UpdateDashboard(string url)
		{
			CrawlQueue.FetchAttributes();
			Status.UpdateStatus(State, (int)CPUCount.NextValue(), (int)MemCount.NextValue(), url, CrawlQueue.ApproximateMessageCount.ToString());
			TableOperation insertOperation = TableOperation.InsertOrReplace(Status);
			AdminStatusTable.ExecuteAsync(insertOperation);
		}

		public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
