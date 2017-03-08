﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Portable
{
	public class URLEntity : TableEntity
	{
		public URLEntity(string URL, string keyword, string title, DateTime date)
		{
			this.PartitionKey = Base64.Base64Encode(keyword);
			this.RowKey = Base64.Base64Encode(title);
			this.URL = URL;
			this.Title = title;
			this.Date = date;
		}

		public URLEntity() { }

		public string URL { get; set; }

		public string Title { get; set; }

		public DateTime Date { get; set; }
	}
}