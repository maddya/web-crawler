using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
	public class TrieNode
	{
		public LinkedList<TrieNode> Children { private set; get; }
		public int Count { private set; get; }
		public char Data { private set; get; }

		public TrieNode(char data = ' ')
		{
			this.Data = data;
			Count = 0;
			Children = new LinkedList<TrieNode>();
		}

		public bool isLeaf()
		{
			return Count > 0;
		}

		public TrieNode GetChild(char c, bool createIfNotExist = false)
		{
			foreach (var child in Children)
				if (child.Data == c)
					return child;

			if (createIfNotExist)
				return CreateChild(c);

			return null;
		}

		public void AddCount()
		{
			Count++;
		}

		public TrieNode CreateChild(char c)
		{
			var child = new TrieNode(c);
			Children.AddLast(child);
			return child;
		}
	}
}