using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Charlotte.Tools
{
	public static class XMLTools
	{
		public class Node
		{
			public string Name;
			public string Value;
			public List<Node> Children = new List<Node>();
			public bool AttributeFlag;

			public void Search(Action<Node, string> reaction, string xmlPath = "", string xmlPathSeparator = "/")
			{
				xmlPath += this.Name;
				reaction(this, xmlPath);
				xmlPath += xmlPathSeparator;

				foreach (Node child in this.Children)
				{
					child.Search(reaction, xmlPath);
				}
			}
		}

		public static Node LoadFromFile(string xmlFile)
		{
			Node node = new Node();
			Stack<Node> parents = new Stack<Node>();

			using (XmlReader reader = XmlReader.Create(xmlFile))
			{
				while (reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							{
								Node child = new Node() { Name = reader.LocalName };

								node.Children.Add(child);
								parents.Push(node);
								node = child;

								bool singleTag = reader.IsEmptyElement;

								while (reader.MoveToNextAttribute())
									node.Children.Add(new Node() { Name = reader.Name, Value = reader.Value, AttributeFlag = true });

								if (singleTag)
									node = parents.Pop();
							}
							break;

						case XmlNodeType.Text:
							node.Value = reader.Value;
							break;

						case XmlNodeType.EndElement:
							node = parents.Pop();
							break;

						default:
							break;
					}
				}
			}
			node = node.Children[0];
			Normalize(node);
			return node;
		}

		private static void Normalize(Node node)
		{
			Queue<Node> q = new Queue<Node>();

			q.Enqueue(node);

			while (1 <= q.Count)
			{
				node = q.Dequeue();

				// 正規化
				{
					node.Name = node.Name ?? "";
					node.Value = node.Value ?? "";

					// XmlReader が &xxx; を変換(復元)してくれる。

					// 名前空間を除去
					{
						int colon = node.Name.IndexOf(':');

						if (colon != -1)
							node.Name = node.Name.Substring(colon + 1);
					}

					node.Name = node.Name.Trim();
					node.Value = node.Value.Trim();
				}

				foreach (Node child in node.Children)
					q.Enqueue(child);
			}
		}

		public static void WriteToFile(Node root, string xmlFile)
		{
			using (StreamWriter writer = new StreamWriter(xmlFile, false, Encoding.UTF8))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				WriteTo(root, writer, 0);
			}
		}

		private static void WriteTo(Node node, StreamWriter writer, int depth)
		{
			string name = node.Name;
			string value = node.Value;

			// ノード(タグ名・値)の正規化
			{
				name = name ?? "";
				value = value ?? "";

				name = EncodeXML(name);
				value = EncodeXML(value);
			}

			writer.Write(Indent(depth) + "<" + name);

			foreach (Node child in node.Children)
				if (child.AttributeFlag)
					WriteAttributeTo(child, writer);

			if (node.Children.Any(child => !child.AttributeFlag))
			{
				writer.WriteLine(">" + node.Value);

				foreach (Node child in node.Children)
					if (!child.AttributeFlag)
						WriteTo(child, writer, depth + 1);

				writer.WriteLine(Indent(depth) + "</" + name + ">");
			}
			else if (value != "")
			{
				writer.WriteLine(">" + node.Value + "</" + name + ">");
			}
			else
			{
				writer.WriteLine("/>");
			}
		}

		private static void WriteAttributeTo(Node node, StreamWriter writer)
		{
			string name = node.Name;
			string value = node.Value;

			// 属性(属性名・属性値)の正規化
			{
				name = name ?? "";
				value = value ?? "";

				name = EncodeXML(name);
				value = EncodeXML(value);
			}

			writer.Write(" " + name + "=\"" + value + "\"");
		}

		private static string Indent(int depth)
		{
			return new string('\t', depth);
		}

		private static string EncodeXML(string str)
		{
			StringBuilder buff = new StringBuilder();

			foreach (char chr in str)
			{
				switch (chr)
				{
					case '"':
						buff.Append("&quot;");
						break;

					case '\'':
						buff.Append("&apos;");
						break;

					case '<':
						buff.Append("&lt;");
						break;

					case '>':
						buff.Append("&gt;");
						break;

					case '&':
						buff.Append("&amp;");
						break;

					default:
						buff.Append(chr);
						break;
				}
			}
			return buff.ToString();
		}
	}
}
