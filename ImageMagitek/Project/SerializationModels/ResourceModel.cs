using System.Collections.Generic;

namespace ImageMagitek.Project.Serialization;

public abstract class ResourceModel {
	public string Name { get; set; }
	public ResourceModel Parent { get; set; }
	internal Dictionary<string, ResourceModel> ChildResources = new();

	public abstract bool ResourceEquals(ResourceModel model);
}
