using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Portable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WorkerRole1
{
	class WebCrawler
	{
		private HashSet<string> VisitedLinks { get; set; }
		private DateTime OldestAllowed { get; set; }
		private List<string> BadExtensions { get; set; }
		private Dictionary<string, List<string>> RobotRules { get; set; }
		private iStorageAccount Storage { get; set; }
		

		public WebCrawler(string[] robots, iStorageAccount storage)
		{
			this.Storage = storage;
			RobotRules = new Dictionary<string, List<string>>();
			ProcessRobots(robots);
			this.VisitedLinks = new HashSet<string>();
			OldestAllowed = new DateTime(2016, 12, 1);
			BadExtensions = new List<string> { ".jpg", ".jpeg", ".gif", ".ico", ".png", ".pdf" };
		}

		private void ProcessRobots(string[] robots)
		{
			foreach (string URL in robots)
			{
				int startIndex = URL.IndexOf("www.") + 4;
				int stopIndex = URL.IndexOf(@"/robots") - 11;
				string baseURL = URL.Substring(startIndex, stopIndex);
				WebClient client = new WebClient();
				string file = client.DownloadString(URL);
				string[] lines = file.Split('\n');
				List<string> rules = new List<string>
					(lines.Where(x => x.ToLower().StartsWith("disallow:"))
					.Select(x => x.Substring(x.IndexOf("/"))));
				RobotRules.Add(baseURL, rules);
			}
		}

		public void ProcessURL(string URL)
		{
			try
			{
				URL = URL.ToLower();
				VisitedLinks.Add(URL);
				if (URL.EndsWith("txt"))
				{
					ProcessTxt(URL);
				}
				else if (URL.EndsWith("xml"))
				{
					ProcessXML(URL);
				}
				else
				{
					ProcessHTML(URL);
				}
			}
			catch (Exception ex)
			{
				Storage.AddErrorMessage(URL);
			}
		}

		private bool CheckIfAllowed(string URL)
		{
			foreach (string key in RobotRules.Keys)
			{
				if (URL.Contains(key))
				{
					foreach (string s in RobotRules[key])
					{
						if (URL.Contains($"{key}{s}"))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		private void ProcessTxt(string URL)
		{
			WebClient client = new WebClient();
			string file = client.DownloadString(URL);
			string[] lines = file.Split('\n');
			HashSet<string> links = new HashSet<string>
				(lines.Where(x => x.ToLower().StartsWith("sitemap:"))
				.Select(x => x.Substring(x.IndexOf("http"))));
			foreach (string link in links)
			{
				if (CheckLinkDomain(link))
				{
					if (!link.Contains("bleacherreport"))
					{
						Storage.AddSitemap(link);
					}
					else if (link.Contains("nba"))
					{
						Storage.AddSitemap(link);
					}
				}
			}
		}

		private void ProcessXML(string URL)
		{
			HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(URL);

			httpRequest.Timeout = 10000;     // 10 secs
			httpRequest.UserAgent = "Code Sample Web Client";

			HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();
			var stream = new StreamReader(webResponse.GetResponseStream());

			XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
			xmlDoc.Load(stream);

			// Get elements
			XmlNodeList elements = xmlDoc.GetElementsByTagName("loc");

			foreach (XmlNode linkElement in elements)
			{
				var link = linkElement.InnerText.ToLower();
				bool correctDate = true;
				Regex dateMatcher = new Regex(@"((\d{4}\/\d{2}\/\d{2})|(\d{4}-\d{2}))");
				Match dateMatch = dateMatcher.Match(link);
				if (dateMatch.Success)
				{
					string date = dateMatch.ToString();
					correctDate = CheckLinkIsRecent(date);
				}
				if (CheckLinkDomain(link) && CheckLinkIsCorrectType(link) && CheckIfAllowed(link) && correctDate)
				{
					if (link.EndsWith("xml"))
					{
						if (!link.Contains("bleacherreport")) {
							Storage.AddSitemap(link);
						} else if (link.Contains("nba"))
						{
							Storage.AddSitemap(link);
						}
 					}
					else
					{
						Storage.AddLinkForParsing(link);
					}
				}
			}
		}

		private void ProcessHTML(string URL)
		{
			HtmlWeb hw = new HtmlWeb();
			HtmlDocument doc = new HtmlDocument();
			doc = hw.Load(URL);

			var title = doc.DocumentNode.Descendants("title").FirstOrDefault().InnerText;
			Storage.AddLinkToResults(URL, title, DateTime.Now);

			foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
			{
				// Get the value of the HREF attribute
				string hrefValue = link.GetAttributeValue("href", string.Empty);
				if (CheckLinkDomain(hrefValue) && CheckIfAllowed(hrefValue) && CheckLinkIsCorrectType(hrefValue))
				{
					Storage.AddLinkForParsing(hrefValue);
				}
			}
		}

		private bool CheckLinkDomain(string URL)
		{
			bool isInDomain = false;
			foreach (string key in RobotRules.Keys)
			{
				if (URL.Contains(key))
				{
					isInDomain = true;
				}
			}
			return isInDomain;
		}

		private bool CheckLinkIsRecent(string date)
		{
			DateTime temp;
			if (DateTime.TryParse(date, out temp))
			{
				return temp >= OldestAllowed;
			}
			else
			{
				Debug.Print(date);
				return true;
			}
		}

		private bool CheckLinkIsCorrectType(string URL)
		{
			foreach (string s in BadExtensions)
			{
				if (URL.EndsWith(s))
				{
					return false;
				}
			}
			return true;
		}
	}
}