﻿using ImageMagitek;
using ImageMagitek.Colors;
using ImageMagitek.Project;

namespace TileShop.WPF.ViewModels;

public class ProjectNodeViewModel : ResourceNodeViewModel {
	public override int SortPriority => 0;

	public ProjectNodeViewModel(ResourceNode node) {
		Node = node;
		Name = node.Name;

		foreach (var child in Node.ChildNodes) {
			ResourceNodeViewModel model;

			if (child.Item is ResourceFolder) {
				model = new FolderNodeViewModel(child, this);
			} else if (child.Item is Palette) {
				model = new PaletteNodeViewModel(child, this);
			} else if (child.Item is DataFile) {
				model = new DataFileNodeViewModel(child, this);
			} else if (child.Item is Arranger) {
				model = new ArrangerNodeViewModel(child, this);
			} else {
				continue;
			}

			Children.Add(model);
		}
	}
}
