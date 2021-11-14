﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using ImageMagitek.ExtensionMethods;

namespace ImageMagitek.Codec;

public sealed class XmlGraphicsFormatReader : IGraphicsFormatReader {
	private readonly XmlSchemaSet _schemas = new XmlSchemaSet();

	public XmlGraphicsFormatReader(string schemaFileName) {
		if (!string.IsNullOrWhiteSpace(schemaFileName)) {
			if (!File.Exists(schemaFileName)) {
				throw new ArgumentException($"{nameof(schemaFileName)} cannot be found");
			} else {
				using var schemaStream = File.OpenRead(schemaFileName);
				_schemas.Add("", XmlReader.Create(schemaStream));
			}
		} else {
			throw new ArgumentException($"{nameof(schemaFileName)} was null or empty");
		}
	}

	public MagitekResults<IGraphicsFormat> LoadFromFile(string fileName) {
		if (!File.Exists(fileName)) {
			return new MagitekResults<IGraphicsFormat>.Failed($"Codec file {fileName} does not exist");
		}

		using var stream = File.OpenRead(fileName);
		var doc = XDocument.Load(stream, LoadOptions.SetLineInfo);

		var validationErrors = new List<string>();

		doc.Validate(_schemas, (o, e) => {
			validationErrors.Add(e.Message);
		});

		if (validationErrors.Any()) {
			validationErrors.Insert(0, $"Codec '{fileName}' failed to be validated");
			return new MagitekResults<IGraphicsFormat>.Failed(validationErrors);
		}

		return doc.Root?.Name.LocalName switch {
			"flowcodec" => ReadFlowCodec(doc.Root),
			"patterncodec" => ReadPatternCodec(doc.Root),
			_ => new MagitekResults<IGraphicsFormat>.Failed($"Unrecognized codec root element '{doc.Root.Name}'")
		};
	}

	private MagitekResults<IGraphicsFormat> ReadFlowCodec(XElement flowElementRoot) {
		var errors = new List<string>();

		var name = flowElementRoot.Attribute("name").Value;

		var codec = new {
			ColorType = flowElementRoot.Element("colortype"),
			ColorDepth = flowElementRoot.Element("colordepth"),
			Layout = flowElementRoot.Element("layout"),
			DefaultWidth = flowElementRoot.Element("defaultwidth"),
			DefaultHeight = flowElementRoot.Element("defaultheight"),
			FixedSize = flowElementRoot.Element("fixedsize"),
			MergePriority = flowElementRoot.Element("mergepriority"),
			Images = flowElementRoot.Element("images")
		};

		if (!int.TryParse(codec.DefaultWidth?.Value, out var defaultWidth)) {
			errors.Add($"Element width could not be parsed on {codec.DefaultWidth.LineNumber()}");
		}

		if (defaultWidth <= 1) {
			errors.Add($"Specified default width is too small on line {codec.DefaultWidth.LineNumber()}");
		}

		if (!int.TryParse(codec.DefaultHeight?.Value, out var defaultHeight)) {
			errors.Add($"Element height could not be parsed on {codec.DefaultHeight.LineNumber()}");
		}

		if (defaultHeight <= 1) {
			errors.Add($"Specified default height is too small on line {codec.DefaultHeight.LineNumber()}");
		}

		PixelColorType colorType = default;
		if (codec.ColorType?.Value == "indexed") {
			colorType = PixelColorType.Indexed;
		} else if (codec.ColorType?.Value == "direct") {
			colorType = PixelColorType.Direct;
		} else {
			errors.Add($"Unrecognized colortype '{codec.ColorType}' on line {codec.ColorType.LineNumber()}");
		}

		if (!int.TryParse(codec.ColorDepth?.Value, out var colorDepth)) {
			errors.Add($"colordepth could not be parsed on {codec.ColorDepth.LineNumber()}");
		}

		if (colorDepth < 1 && colorDepth > 32) {
			errors.Add($"colordepth contains an out of range value '{colorDepth}' on {codec.ColorDepth.LineNumber()}");
		}

		ImageLayout layout = default;
		if (codec.Layout?.Value == "tiled") {
			layout = ImageLayout.Tiled;
		} else if (codec.Layout?.Value == "single") {
			layout = ImageLayout.Single;
		} else {
			errors.Add($"Unrecognized layout '{codec.Layout?.Value}' on line {codec.Layout.LineNumber()}");
		}

		var fixedSize = false;
		if (codec.FixedSize?.Value == "true") {
			fixedSize = true;
		} else if (codec.FixedSize?.Value == "false") {
			fixedSize = false;
		} else {
			errors.Add($"Unrecognized fixedsize value '{codec.FixedSize?.Value}' on line {codec.FixedSize.LineNumber()}");
		}

		var format = new FlowGraphicsFormat(name, colorType, colorDepth, layout, defaultWidth, defaultHeight) {
			FixedSize = fixedSize
		};

		var mergeString = codec.MergePriority?.Value ?? "";
		mergeString = mergeString.Replace(" ", "");
		var mergeItems = mergeString.Split(',');

		if (mergeItems.Length == format.ColorDepth) {
			format.MergePlanePriority = new int[format.ColorDepth];

			for (var i = 0; i < mergeItems.Length; i++) {
				if (int.TryParse(mergeItems[i], out var mergePlane)) {
					format.MergePlanePriority[i] = mergePlane;
				} else {
					errors.Add($"Merge priority '{mergeItems[i]}' could not be parsed on {codec.MergePriority.LineNumber()}");
				}
			}
		} else {
			errors.Add($"The number of entries in mergepriority does not match the colordepth on line {codec.MergePriority.LineNumber()}");
		}

		var imageElements = flowElementRoot.Element("images").Elements("image");
		var images = imageElements.Select(x => new {
			ColorDepth = x.Element("colordepth"),
			RowInterlace = x.Element("rowinterlace"),
			RowPixelPattern = x.Element("rowpixelpattern")
		});

		foreach (var image in images) {
			int[] rowPixelPattern;

			if (image.RowPixelPattern is not null) // Parse rowpixelpattern
			{
				var patternString = image.RowPixelPattern.Value;
				patternString = patternString.Replace(" ", "");
				var patternInputs = patternString.Split(',');

				rowPixelPattern = new int[patternInputs.Length];

				for (var i = 0; i < patternInputs.Length; i++) {
					if (int.TryParse(patternInputs[i], out var patternPriority)) {
						rowPixelPattern[i] = patternPriority;
					} else {
						errors.Add($"rowpixelpattern value '{patternInputs[i]}' could not be parsed on {image.RowPixelPattern.LineNumber()}");
					}
				}
			} else // Create a default rowpixelpattern
			  {
				rowPixelPattern = new int[1];
			}

			if (!int.TryParse(image.ColorDepth.Value, out var imageColorDepth)) {
				errors.Add($"colordepth could not be parsed on {codec.DefaultHeight.LineNumber()}");
			}

			if (!bool.TryParse(image.RowInterlace.Value, out var imageRowInterlace)) {
				errors.Add($"rowinterlace could not be parsed on {codec.DefaultHeight.LineNumber()}");
			}

			var ip = new ImageProperty(imageColorDepth, imageRowInterlace, rowPixelPattern);
			format.ImageProperties.Add(ip);
		}

		var sumColorDepth = format.ImageProperties.Sum(x => x.ColorDepth);
		if (format.ColorDepth != sumColorDepth) {
			errors.Add($"Codec's colordepth '{format.ColorDepth}' does not match the sum of all image colordepth elements '{sumColorDepth}'");
		}

		if (errors.Any()) {
			return new MagitekResults<IGraphicsFormat>.Failed(errors);
		} else {
			return new MagitekResults<IGraphicsFormat>.Success(format);
		}
	}

	private MagitekResults<IGraphicsFormat> ReadPatternCodec(XElement patternElementRoot) {
		var errors = new List<string>();

		var name = patternElementRoot.Attribute("name").Value;

		var codec = new {
			ColorType = patternElementRoot.Element("colortype"),
			ColorDepth = patternElementRoot.Element("colordepth"),
			Layout = patternElementRoot.Element("layout"),
			Packing = patternElementRoot.Element("packing"),
			MergePriority = patternElementRoot.Element("mergepriority"),
			RowPixelPattern = patternElementRoot.Element("rowpixelpattern"),
			Width = patternElementRoot.Element("width"),
			Height = patternElementRoot.Element("height"),
			Patterns = patternElementRoot.Element("patterns")
		};

		if (!int.TryParse(codec.Width?.Value, out var width)) {
			errors.Add($"Element width could not be parsed on {codec.Width.LineNumber()}");
		}

		if (width <= 1) {
			errors.Add($"Specified width is too small on line {codec.Width.LineNumber()}");
		}

		if (!int.TryParse(codec.Height?.Value, out var height)) {
			errors.Add($"Element height could not be parsed on {codec.Height.LineNumber()}");
		}

		if (height <= 1) {
			errors.Add($"Specified height is too small on line {codec.Height.LineNumber()}");
		}

		PixelColorType colorType = default;
		if (codec.ColorType?.Value == "indexed") {
			colorType = PixelColorType.Indexed;
		} else if (codec.ColorType?.Value == "direct") {
			colorType = PixelColorType.Direct;
		} else {
			errors.Add($"Unrecognized colortype '{codec.ColorType}' on line {codec.ColorType.LineNumber()}");
		}

		if (!int.TryParse(codec.ColorDepth?.Value, out var colorDepth)) {
			errors.Add($"colordepth could not be parsed on {codec.ColorDepth.LineNumber()}");
		}

		if (colorDepth < 1 && colorDepth > 32) {
			errors.Add($"colordepth contains an out of range value '{colorDepth}' on {codec.ColorDepth.LineNumber()}");
		}

		ImageLayout layout = default;
		if (codec.Layout?.Value == "tiled") {
			layout = ImageLayout.Tiled;
		} else if (codec.Layout?.Value == "single") {
			layout = ImageLayout.Single;
		} else {
			errors.Add($"Unrecognized layout '{codec.Layout?.Value}' on line {codec.Layout.LineNumber()}");
		}

		PixelPacking packing = default;
		if (codec.Packing?.Value == "planar") {
			packing = PixelPacking.Planar;
		} else if (codec.Packing?.Value == "chunky") {
			packing = PixelPacking.Chunky;
		} else {
			errors.Add($"Unrecognized layout '{codec.Packing?.Value}' on line {codec.Packing.LineNumber()}");
		}

		var format = new PatternGraphicsFormat(name, colorType, colorDepth, layout, packing, width, height);

		int[] rowPixelPattern;
		var patternString = codec.RowPixelPattern?.Value;
		if (patternString is not null) {
			patternString = patternString.Replace(" ", "");
			var patternInputs = patternString.Split(',');

			rowPixelPattern = new int[patternInputs.Length];

			for (var i = 0; i < patternInputs.Length; i++) {
				if (int.TryParse(patternInputs[i], out var patternPriority)) {
					rowPixelPattern[i] = patternPriority;
				} else {
					errors.Add($"rowpixelpattern value '{patternInputs[i]}' could not be parsed on {codec.RowPixelPattern.LineNumber()}");
				}
			}
		} else // Create a default rowpixelpattern
		  {
			rowPixelPattern = new int[1];
		}

		format.RowPixelPattern = new RepeatList(rowPixelPattern);

		var mergeString = codec.MergePriority?.Value ?? "";
		mergeString = mergeString.Replace(" ", "");
		var mergeItems = mergeString.Split(',');

		if (mergeItems.Length == format.ColorDepth) {
			format.MergePlanePriority = new int[format.ColorDepth];

			for (var i = 0; i < mergeItems.Length; i++) {
				if (int.TryParse(mergeItems[i], out var mergePlane)) {
					format.MergePlanePriority[i] = mergePlane;
				} else {
					errors.Add($"Merge priority '{mergeItems[i]}' could not be parsed on {codec.MergePriority.LineNumber()}");
				}
			}
		} else {
			errors.Add($"The number of entries in mergepriority does not match the colordepth on line {codec.MergePriority.LineNumber()}");
		}

		var sizeElement = patternElementRoot.Element("patterns").Attribute("size");

		if (!int.TryParse(sizeElement?.Value, out var patternSize)) {
			errors.Add($"Pattern size could not be parsed on {codec.Width.LineNumber()}");
		}

		if (patternSize < 1 || patternSize > PatternList.MaxPatternSize) {
			errors.Add($"Pattern size contains an out of range value '{patternSize}' {codec.Width.LineNumber()}");
		}

		var patternStrings = patternElementRoot
			.Element("patterns")
			.Elements("pattern")
			.Select(x => string.Join("", x.Value.Where(c => !char.IsWhiteSpace(c))))
			.ToArray();

		var patternResult = PatternList.TryCreatePatternList(patternStrings, packing, width, height, colorDepth, patternSize);

		patternResult.Switch(
			success => {
				format.SetPattern(success.Result);
			},
			failed => errors.Add(failed.Reason));

		if (errors.Any()) {
			return new MagitekResults<IGraphicsFormat>.Failed(errors);
		} else {
			return new MagitekResults<IGraphicsFormat>.Success(format);
		}
	}
}
