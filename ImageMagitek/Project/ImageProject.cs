﻿using System.Collections.Generic;
using System.Linq;

namespace ImageMagitek.Project;

public sealed class ImageProject : IProjectResource {
	public string Name { get; set; }
	public string Root { get; set; }

	public bool CanContainChildResources => true;

	public bool ShouldBeSerialized { get; set; } = true;

	public ImageProject() : this("") { }

	public ImageProject(string name) {
		Name = name;
		Root = "";
	}

	public void Rename(string name) => Name = name;

	public bool UnlinkResource(IProjectResource resource) => false;

	public IEnumerable<IProjectResource> LinkedResources => Enumerable.Empty<IProjectResource>();
}
