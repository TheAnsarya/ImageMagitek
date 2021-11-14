﻿using System.Collections.Generic;
using System.Linq;

namespace ImageMagitek.Project;

public sealed class ResourceFolder : IProjectResource {
	public ResourceFolder() : this("") { }

	public ResourceFolder(string name) => Name = name;

	public string Name { get; set; }

	public bool CanContainChildResources => true;

	public bool ShouldBeSerialized { get; set; } = true;

	public bool UnlinkResource(IProjectResource resource) => false;

	public IEnumerable<IProjectResource> LinkedResources => Enumerable.Empty<IProjectResource>();
}
