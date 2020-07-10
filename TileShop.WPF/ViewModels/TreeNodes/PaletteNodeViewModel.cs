﻿using ImageMagitek.Project;
using Monaco.PathTree;

namespace TileShop.WPF.ViewModels
{
    public class PaletteNodeViewModel : TreeNodeViewModel
    {
        public override int SortPriority => 2;

        public PaletteNodeViewModel(IPathTreeNode<IProjectResource> node, TreeNodeViewModel parent)
        {
            Node = node;
            Name = node.Name;
            Type = GetType();
            ParentModel = parent;
        }
    }
}
