﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace Portable
{
	public static class CloudConfiguration
	{
		private static readonly CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
					ConfigurationManager.AppSettings["StorageConnectionString"]);

		public static CloudQueue GetLoadingQueue()
		{
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference("sitemapqueue");
			queue.CreateIfNotExists();
			return queue;
		}

		public static CloudQueue GetCrawlingQueue()
		{
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference("linkqueue");
			queue.CreateIfNotExists();
			return queue;
		}

		public static CloudQueue GetStopQueue()
		{
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference("stopqueue");
			queue.CreateIfNotExists();
			return queue;
		}

		public static CloudQueue GetStateQueue()
		{
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference("statequeue");
			queue.CreateIfNotExists();
			return queue;
		}

		public static CloudQueue GetErrorQueue()
		{
			CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
			CloudQueue queue = queueClient.GetQueueReference("errorqueue");
			queue.CreateIfNotExists();
			return queue;
		}

		public static CloudTable GetSiteDataTable()
		{
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			CloudTable table = tableClient.GetTableReference("sitedatatable");
			table.CreateIfNotExists();
			return table;
		}

		public static CloudTable GetAdminStatusTable()
		{
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			CloudTable table = tableClient.GetTableReference("adminstatustable");
			table.CreateIfNotExists();
			return table;
		}

		public static CloudTable GetErrorTable()
		{
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
			CloudTable table = tableClient.GetTableReference("errortable");
			table.CreateIfNotExists();
			return table;
		}

	}
}