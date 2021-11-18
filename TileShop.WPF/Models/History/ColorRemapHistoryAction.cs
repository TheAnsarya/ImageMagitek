using System.Collections.Generic;
using Stylet;

namespace TileShop.WPF.Models;

public class ColorRemapHistoryAction : HistoryAction {
	public override string Name => "ColorRemap";

	private BindableCollection<RemappableColorModel> _initialColors = new();
	public BindableCollection<RemappableColorModel> InitialColors {
		get => _initialColors;
		set => SetAndNotify(ref _initialColors, value);
	}

	private BindableCollection<RemappableColorModel> _finalColors = new();
	public BindableCollection<RemappableColorModel> FinalColors {
		get => _finalColors;
		set => SetAndNotify(ref _finalColors, value);
	}

	public ColorRemapHistoryAction(IList<RemappableColorModel> initialColors, IList<RemappableColorModel> finalColors) {
		_initialColors.AddRange(initialColors);
		_finalColors.AddRange(finalColors);
	}
}
