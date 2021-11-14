﻿using ImageMagitek.Services;
using TileShop.CLI.Porters;

namespace TileShop.CLI.Commands;

public class ExportHandler : ProjectCommandHandler<ExportOptions> {
	public ExportHandler(IProjectService projectService) :
		base(projectService) {
	}

	public override ExitCode Execute(ExportOptions options) {
		var project = OpenProject(options.ProjectFileName);

		if (project is null) {
			return ExitCode.ProjectOpenError;
		}

		foreach (var resourceKey in options.ResourceKeys) {
			_ = Exporter.ExportArranger(project, resourceKey, options.ExportDirectory, options.ForceOverwrite);
		}

		return ExitCode.Success;
	}
}
