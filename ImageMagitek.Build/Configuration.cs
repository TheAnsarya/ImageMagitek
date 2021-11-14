using System.ComponentModel;
using Nuke.Common.Tooling;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public class Configuration : Enumeration {
	public static Configuration Debug { get; } = new Configuration { Value = nameof(Debug) };

	public static Configuration Release { get; } = new Configuration { Value = nameof(Release) };

	public static implicit operator string(Configuration configuration) => configuration.Value;
}
