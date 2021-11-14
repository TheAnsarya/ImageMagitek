﻿using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ImageMagitek.ExtensionMethods;

public static class XElementExtensions {
	/// <summary>
	/// Gets the project path for the specified node
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static string NodePath(this XElement node) {
		var path = "";

		var currentNode = node;
		while (currentNode.Parent != null && currentNode.Parent.Name != "gdf") {
			currentNode = currentNode.Parent;
			path = Path.Combine(currentNode.Attribute("name").Value, path);
		}

		return path;
	}

	/// <summary>
	/// Gets the project key for the specified node
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static string NodeKey(this XElement node) {
		var path = node.NodePath();
		return Path.Combine(path, node.Attribute("name").Value);
	}

	public static int? LineNumber(this XElement element) {
		if (element is null) {
			return default;
		}

		var info = element as IXmlLineInfo;
		return info.HasLineInfo() ? info.LineNumber : default;
	}
}
