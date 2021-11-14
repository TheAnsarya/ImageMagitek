﻿using System.Linq;
using ImageMagitek;
using ImageMagitek.Services;
using Monaco.PathTree;
using TileShop.CLI.Porters;

namespace TileShop.CLI.Commands;

public class ExportAllHandler : ProjectCommandHandler<ExportAllOptions> {
	public ExportAllHandler(IProjectService projectService) :
		base(projectService) {
	}

	public override ExitCode Execute(ExportAllOptions options) {
		var projectTree = OpenProject(options.ProjectFileName);

		if (projectTree is null) {
			return ExitCode.ProjectOpenError;
		}

		foreach (var node in projectTree.EnumerateDepthFirst().Where(x => x.Item is ScatteredArranger)) {
			_ = Exporter.ExportArranger(projectTree, projectTree.CreatePathKey(node), options.ExportDirectory, options.ForceOverwrite);
		}

		return ExitCode.Success;
	}
}
