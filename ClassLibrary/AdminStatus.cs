using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable
{
	public class AdminStatus : TableEntity
	{
		public string State { get; set; }
		public int CPUCounter { get; set; }
		public int MemCounter { get; set; }
		public int NumCrawled { get; set; }
		public List<string> Last10CrawledList { get; set; }
		public string Last10Crawled { get; set; }
		public string QueueSize { get; set; }


		public AdminStatus() { }

		public AdminStatus(string state, int cpu, int mem)
		{
			this.PartitionKey = "Dashboard Status";
			this.RowKey = "Row";
			State = state;
			CPUCounter = cpu;
			MemCounter = mem;
			NumCrawled = 0;
			Last10CrawledList = new List<string>();
			Last10Crawled = "";
			CloudQueue temp = CloudConfiguration.GetCrawlingQueue();
			temp.FetchAttributes();
			QueueSize = temp.ApproximateMessageCount.ToString();
		}

		private void UpdateLastCrawled(string link)
		{
			if (Last10CrawledList.Count >= 10)
			{
				Last10CrawledList.RemoveAt(0);
			}
			Last10CrawledList.Add(link);
			Last10Crawled = "";
			foreach(string url in  Last10CrawledList)
			{
				Last10Crawled += url + " ";
			}
		}

		public void UpdateStatus(string state, int cpu, int mem, string link, string queueSize)
		{
			State = state;
			CPUCounter = cpu;
			MemCounter = mem;
			UpdateLastCrawled(link);
			NumCrawled++;
			QueueSize = queueSize;
		}
	}
}