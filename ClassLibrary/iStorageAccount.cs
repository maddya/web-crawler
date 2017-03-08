using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable
{
	public interface iStorageAccount
	{
		void AddLinkToResults(string URL, string title, DateTime date);
		void AddLinkForParsing(string URL);
		void AddSitemap(string URL);
	}
}
