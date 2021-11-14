namespace ColorPicker.Models;

internal class SecondColorDecorator : IColorStateStorage {
	public ColorState ColorState {
		get => storage.SecondColorState;
		set => storage.SecondColorState = value;
	}
	private readonly ISecondColorStorage storage;
	public SecondColorDecorator(ISecondColorStorage storage) => this.storage = storage;
}
