using System.Diagnostics;

namespace TileShop.WPF.Services;

public interface IDiskExploreService {
	void ExploreDiskLocation(string location);
}

public class DiskExploreService : IDiskExploreService {
	public void ExploreDiskLocation(string location) {
		var command = $"explorer.exe";
		var args = $"/select, {location}";
		_ = Process.Start(command, args);
	}
}
