﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using ImageMagitek.Codec;
using ImageMagitek.Colors;

namespace ImageMagitek.Project.Serialization;

public sealed class XmlProjectSerializerFactory : IProjectSerializerFactory {
	public List<IProjectResource> GlobalResources { get; }

	private readonly string _resourceSchemaFileName;
	private readonly ICodecFactory _codecFactory;
	private readonly IColorFactory _colorFactory;

	public XmlProjectSerializerFactory(string resourceSchemaFileName,
		ICodecFactory codecFactory, IColorFactory colorFactory, IEnumerable<IProjectResource> globalResources) {
		_resourceSchemaFileName = resourceSchemaFileName;
		_codecFactory = codecFactory;
		_colorFactory = colorFactory;
		GlobalResources = globalResources.ToList();
	}

	public IProjectReader CreateReader() {
		var resourceSchemas = CreateSchemas(_resourceSchemaFileName);

		return new XmlProjectReader(resourceSchemas, _codecFactory, _colorFactory, GlobalResources);
	}

	public IProjectWriter CreateWriter(ProjectTree tree) => new XmlProjectWriter(tree, _colorFactory, GlobalResources);

	private XmlSchemaSet CreateSchemas(string resourceSchemaFileName) {
		var resourceSchemaText = File.ReadAllText(resourceSchemaFileName);

		var resourceSchema = XmlSchema.Read(new StringReader(resourceSchemaText), null);

		var resourceSchemaSet = new XmlSchemaSet();
		_ = resourceSchemaSet.Add(resourceSchema);

		return resourceSchemaSet;
	}
}
