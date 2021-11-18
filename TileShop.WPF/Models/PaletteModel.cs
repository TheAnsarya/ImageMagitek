﻿using System;
using System.Windows.Media;
using ImageMagitek.Colors;
using Stylet;

namespace TileShop.WPF.Models;

public class PaletteModel : PropertyChangedBase {
	private string _name;
	public string Name {
		get => _name;
		set => SetAndNotify(ref _name, value);
	}

	private BindableCollection<PaletteEntry> _colors = new();
	public BindableCollection<PaletteEntry> Colors {
		get => _colors;
		set => SetAndNotify(ref _colors, value);
	}

	public Palette Palette { get; }

	public PaletteModel(Palette pal) : this(pal, pal.Entries) { }

	public PaletteModel(Palette pal, int maxColors) {
		Name = pal.Name;
		Palette = pal;

		var colorCount = Math.Min(pal.Entries, maxColors);

		for (var i = 0; i < colorCount; i++) {
			var color = new Color {
				R = pal[i].R,
				G = pal[i].G,
				B = pal[i].B,
				A = pal[i].A
			};

			var entry = new PaletteEntry((byte)i, color);
			Colors.Add(entry);
		}
	}
}
