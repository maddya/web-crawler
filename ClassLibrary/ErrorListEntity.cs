using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable
{
	public class ErrorListEntity : TableEntity
	{
		public string AllErrors { get; set; }

		public ErrorListEntity() {
			AllErrors = "";
		}

		public void AddAnError(string error)
		{
			AllErrors += " " + error;
			AllErrors = AllErrors.Trim();
		}
	}
}
