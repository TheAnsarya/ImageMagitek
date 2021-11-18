﻿using System;
using System.Collections.Generic;
using System.Linq;
using ImageMagitek.Colors;

namespace ImageMagitek.Project.Serialization;

public static class SerializationMapperExtensions {
	public static DataFile MapToResource(this DataFileModel df) =>
		new(df.Name, df.Location);

	public static DataFileModel MapToModel(this DataFile df) => new() {
		Name = df.Name,
		Location = df.Location
	};

	public static ImageProject MapToResource(this ImageProjectModel model) => new() {
		Name = model.Name,
		Root = model.Root
	};

	public static ImageProjectModel MapToModel(this ImageProject project) => new() {
		Name = project.Name,
		Root = project.Root
	};

	public static PaletteModel MapToModel(this Palette pal, Dictionary<IProjectResource, string> resourceMap, IColorFactory colorFactory) {
		var size = colorFactory.CreateColor(pal.ColorModel).Size;

		var model = new PaletteModel() {
			Name = pal.Name,
			ColorModel = pal.ColorModel,
			ZeroIndexTransparent = pal.ZeroIndexTransparent,
		};

		if (pal.DataFile is not null && resourceMap.TryGetValue(pal.DataFile, out var dataFileKey)) {
			model.DataFileKey = dataFileKey;
		}

		var i = 0;
		while (i < pal.ColorSources.Length) {
			if (pal.ColorSources[i] is FileColorSource fileSource) {
				var sources = pal.ColorSources.Skip(i)
					.TakeWhile((x, i) => x is FileColorSource && (x as FileColorSource).Offset == (fileSource.Offset + (i * size)))
					.ToList();

				var sourceModel = new FileColorSourceModel(fileSource.Offset, sources.Count, fileSource.Endian);
				model.ColorSources.Add(sourceModel);

				i += sources.Count;
			} else if (pal.ColorSources[i] is ProjectNativeColorSource nativeSource) {
				var nativeModel = new ProjectNativeColorSourceModel(nativeSource.Value);
				model.ColorSources.Add(nativeModel);
				i++;
			} else if (pal.ColorSources[i] is ProjectForeignColorSource foreignSource) {
				var foreignModel = new ProjectForeignColorSourceModel(foreignSource.Value);
				model.ColorSources.Add(foreignModel);
				i++;
			} else if (pal.ColorSources[i] is ScatteredColorSource scatteredSource) {
			}
		}

		return model;
	}

	public static Palette MapToResource(this PaletteModel model, IColorFactory colorFactory) {
		var size = colorFactory.CreateColor(model.ColorModel).Size;
		var sources = new List<IColorSource>();

		foreach (var source in model.ColorSources) {
			if (source is FileColorSourceModel fileSource) {
				var fileSources = Enumerable.Range(0, fileSource.Entries)
					.Select(x => fileSource.FileAddress + (size * x))
					.Select(x => new FileColorSource(x, fileSource.Endian))
					.ToList();
				sources.AddRange(fileSources);
			} else if (source is ProjectNativeColorSourceModel nativeSource) {
				sources.Add(new ProjectNativeColorSource(nativeSource.Value));
			} else if (source is ProjectForeignColorSourceModel foreignSource) {
				sources.Add(new ProjectForeignColorSource(foreignSource.Value));
			}
			//else if (source is ScatteredColorSourceModel scatteredSource)
			//{

			//}
		}

		return new Palette(model.Name, colorFactory, model.ColorModel, sources, model.ZeroIndexTransparent, model.StorageSource);
	}

	public static ResourceFolderModel MapToModel(this ResourceFolder folder) => new() {
		Name = folder.Name
	};

	public static ResourceFolder MapToResource(this ResourceFolderModel model) =>
		new(model.Name);

	public static ScatteredArrangerModel MapToModel(this ScatteredArranger arranger, Dictionary<IProjectResource, string> resourceMap) {
		var model = new ScatteredArrangerModel() {
			Name = arranger.Name,
			ArrangerElementSize = arranger.ArrangerElementSize,
			ElementPixelSize = arranger.ElementPixelSize,
			Layout = arranger.Layout,
			ColorType = arranger.ColorType
		};

		model.ElementGrid = new ArrangerElementModel[model.ArrangerElementSize.Width, model.ArrangerElementSize.Height];

		for (var x = 0; x < model.ElementGrid.GetLength(0); x++) {
			for (var y = 0; y < model.ElementGrid.GetLength(1); y++) {
				if (arranger.GetElement(x, y) is ArrangerElement el) {
					model.ElementGrid[x, y] = MapToModel(el, x, y);
				}
			}
		}

		return model;

		ArrangerElementModel MapToModel(ArrangerElement el, int elemX, int elemY) {
			var model = new ArrangerElementModel {
				FileAddress = el.FileAddress,
				PositionX = elemX,
				PositionY = elemY,
				CodecName = el.Codec.Name,
				Mirror = el.Mirror,
				Rotation = el.Rotation
			};

			if (el.DataFile is not null && resourceMap.TryGetValue(el.DataFile, out var dataFileKey)) {
				model.DataFileKey = dataFileKey;
			}

			if (el.Palette is not null && resourceMap.TryGetValue(el.Palette, out var paletteKey)) {
				model.PaletteKey = paletteKey;
			}

			return model;
		}
	}

	/// <summary>
	/// Returns the enumeration of all Elements in the grid in a left-to-right, row-by-row order
	/// </summary>
	/// <returns></returns>
	public static IEnumerable<ArrangerElementModel> EnumerateElements(this ScatteredArrangerModel model) =>
		model.EnumerateElements(0, 0, model.ArrangerElementSize.Width, model.ArrangerElementSize.Height);

	/// <summary>
	/// Returns the enumeration of a subsection of Elements in the grid in a left-to-right, row-by-row order
	/// </summary>
	/// <param name="elemX">Starting x-coordinate in element coordinates</param>
	/// <param name="elemY">Starting y-coordinate in element coordinates</param>
	/// <param name="width">Number of elements to enumerate in x-direction</param>
	/// <param name="height">Number of elements to enumerate in y-direction</param>
	/// <returns></returns>
	public static IEnumerable<ArrangerElementModel> EnumerateElements(this ScatteredArrangerModel model,
		int elemX, int elemY, int width, int height) {
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				yield return model.ElementGrid[x + elemX, y + elemY];
			}
		}
	}

	public static string FindMostFrequentElementPropertyValue(this ScatteredArrangerModel model, Func<ArrangerElementModel, string> selector) => EnumerateElements(model)
			.OfType<ArrangerElementModel>()
			.Select(selector)
			.GroupBy(x => x)
			.Select(group => new {
				Item = group.Key,
				Count = group.Count()
			})
			.MaxBy(x => x.Count)
			?.Item;
}
