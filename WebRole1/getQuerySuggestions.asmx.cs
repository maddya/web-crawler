using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Diagnostics;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Runtime.Caching;

namespace WebRole1
{
	/// <summary>
	/// Summary description for getQuerySuggestions
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	[System.Web.Script.Services.ScriptService]
	public class getQuerySuggestions : System.Web.Services.WebService
	{

		public MemoryStream memStream;
		public PerformanceCounter memCounter;
		public static Trie data;
		public MemoryCache memCache;


		[WebMethod]
		public string DownloadWiki()
		{
			memCache = MemoryCache.Default;
			if (!memCache.Contains("data"))
			{
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
				ConfigurationManager.AppSettings["maddyaustin_AzureStorageConnectionString"]);
				CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
				CloudBlobContainer container = blobClient.GetContainerReference("blob2");
				if (container.Exists())
				{
					foreach (IListBlobItem item in container.ListBlobs(null, false))
					{
						if (item.GetType() == typeof(CloudBlockBlob))
						{
							CloudBlockBlob blob = (CloudBlockBlob)item;
							memStream = new MemoryStream();
							blob.DownloadToStream(memStream);
							memStream.Position = 0;
							return BuildTrie();
						}
					}
				}
			}
			else
			{
				data = (Trie)memCache.Get("data", null);
			}
			return "could not create Trie";
		}

		[WebMethod]
		public string BuildTrie()
		{
			data = new Trie();
			StreamReader sr = new StreamReader(memStream);
			memCounter = new PerformanceCounter("Memory", "Available MBytes");
			bool hasMemory = true;

			int i = 0;
			string nextWord = "";
			while (hasMemory)
			//while (i < 10000)
			{
				i++;
				nextWord = sr.ReadLine().ToLower();
				data.AddWord(nextWord);
				if (i % 1000 == 0)
				{
					hasMemory = memCounter.NextValue() > 20;
				}
			}
			memCache.Set("data", data, null);
			return ("last word: " + nextWord + ", memory left: " + memCounter.NextValue() + ", i: " + i);
		}

		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public string SearchTrie(string input)
		{
			input = input.ToLower();
			List<string> results = data.Search(input);
			return new JavaScriptSerializer().Serialize(results);
		}

	}
}