﻿using System;
using System.Linq;
using ImageMagitek;
using ImageMagitek.Colors;
using ImageMagitek.Image;
using ImageMagitek.Services;
using Stylet;
using TileShop.Shared.EventModels;
using TileShop.Shared.Models;
using TileShop.WPF.Imaging;
using TileShop.WPF.Models;
using Point = System.Drawing.Point;

namespace TileShop.WPF.ViewModels;

public class IndexedPixelEditorViewModel : PixelEditorViewModel<byte> {
	private IndexedImage _indexedImage;

	private BindableCollection<PaletteModel> _palettes = new BindableCollection<PaletteModel>();
	public BindableCollection<PaletteModel> Palettes {
		get => _palettes;
		set => SetAndNotify(ref _palettes, value);
	}

	private PaletteModel _activePalette;
	public PaletteModel ActivePalette {
		get => _activePalette;
		set => SetAndNotify(ref _activePalette, value);
	}

	public IndexedPixelEditorViewModel(Arranger arranger, Arranger projectArranger, IEventAggregator events,
		IWindowManager windowManager, IPaletteService paletteService)
		: base(projectArranger, events, windowManager, paletteService) => Initialize(arranger, 0, 0, arranger.ArrangerPixelSize.Width, arranger.ArrangerPixelSize.Height);

	public IndexedPixelEditorViewModel(Arranger arranger, Arranger projectArranger, int viewX, int viewY, int viewWidth, int viewHeight,
		IEventAggregator events, IWindowManager windowManager, IPaletteService paletteService)
		: base(projectArranger, events, windowManager, paletteService) => Initialize(arranger, viewX, viewY, viewWidth, viewHeight);

	private void Initialize(Arranger arranger, int viewX, int viewY, int viewWidth, int viewHeight) {
		Resource = arranger;
		WorkingArranger = arranger.CloneArranger();
		_viewX = viewX;
		_viewY = viewY;
		_viewWidth = viewWidth;
		_viewHeight = viewHeight;

		var maxColors = WorkingArranger.EnumerateElementsWithinPixelRange(viewX, viewY, viewWidth, viewHeight)
			.OfType<ArrangerElement>()
			.Select(x => 1 << x.Codec.ColorDepth)
			.Max();

		var arrangerPalettes = WorkingArranger.EnumerateElementsWithinPixelRange(viewX, viewY, viewWidth, viewHeight)
			.OfType<ArrangerElement>()
			.Select(x => x.Palette)
			.Distinct()
			.OrderBy(x => x.Name)
			.Select(x => new PaletteModel(x, Math.Min(maxColors, x.Entries)));

		Palettes = new BindableCollection<PaletteModel>(arrangerPalettes);

		_indexedImage = new IndexedImage(WorkingArranger, _viewX, _viewY, _viewWidth, _viewHeight);
		BitmapAdapter = new IndexedBitmapAdapter(_indexedImage);

		DisplayName = $"Pixel Editor - {WorkingArranger.Name}";
		SnapMode = SnapMode.Pixel;
		Selection = new ArrangerSelection(arranger, SnapMode);

		CreateGridlines();

		ActivePalette = Palettes.First();
		PrimaryColor = 0;
		SecondaryColor = 1;
		NotifyOfPropertyChange(() => CanRemapColors);
	}

	public override void Render() => BitmapAdapter.Invalidate();

	protected override void ReloadImage() => _indexedImage.Render();

	protected override void CreateGridlines() {
		if (WorkingArranger is null) {
			return;
		}

		if (WorkingArranger.Layout == ElementLayout.Single) {
			CreateGridlines(0, 0, _viewWidth, _viewHeight, 8, 8);
		} else if (WorkingArranger.Layout == ElementLayout.Tiled) {
			var location = WorkingArranger.PointToElementLocation(new Point(_viewX, _viewY));

			var x = WorkingArranger.ElementPixelSize.Width - (_viewX - (location.X * WorkingArranger.ElementPixelSize.Width));
			var y = WorkingArranger.ElementPixelSize.Height - (_viewY - (location.Y * WorkingArranger.ElementPixelSize.Height));

			CreateGridlines(x, y, _viewWidth, _viewHeight,
				WorkingArranger.ElementPixelSize.Width, WorkingArranger.ElementPixelSize.Height);
		}
	}

	#region Commands
	public void ConfirmPendingOperation() {
		if (Paste?.Copy is ElementCopy or IndexedPixelCopy or DirectPixelCopy) {
			ApplyPaste(Paste);
		}
	}

	public override void ApplyPaste(ArrangerPaste paste) {
		var notifyEvent = ApplyPasteInternal(paste).Match(
			success => {
				AddHistoryAction(new PasteArrangerHistoryAction(Paste));

				IsModified = true;
				CancelOverlay();
				BitmapAdapter.Invalidate();

				return new NotifyOperationEvent("Paste successfully applied");
			},
			fail => new NotifyOperationEvent(fail.Reason)
			);

		_events.PublishOnUIThread(notifyEvent);
	}

	public bool CanRemapColors {
		get {
			var palettes = WorkingArranger?.GetReferencedPalettes();
			if (palettes?.Count <= 1) {
				return WorkingArranger.GetReferencedCodecs().All(x => x.ColorType == PixelColorType.Indexed);
			}

			return false;
		}
	}

	public void RemapColors() {
		var palette = WorkingArranger.GetReferencedPalettes().FirstOrDefault();
		if (palette is null) {
			palette = _paletteService.DefaultPalette;
		}

		var maxArrangerColors = WorkingArranger.EnumerateElements().OfType<ArrangerElement>().Select(x => x.Codec?.ColorDepth ?? 0).Max();
		var colors = Math.Min(256, 1 << maxArrangerColors);

		var remapViewModel = new ColorRemapViewModel(palette, colors, _paletteService.ColorFactory);
		if (_windowManager.ShowDialog(remapViewModel) is true) {
			var remap = remapViewModel.FinalColors.Select(x => (byte)x.Index).ToList();
			_indexedImage.RemapColors(remap);
			Render();

			var remapAction = new ColorRemapHistoryAction(remapViewModel.InitialColors, remapViewModel.FinalColors);
			UndoHistory.Add(remapAction);
			IsModified = true;
		}
	}

	public override void PickColor(int x, int y, ColorPriority priority) {
		var el = WorkingArranger.GetElementAtPixel(x, y);

		if (el is ArrangerElement element) {
			ActivePalette = Palettes.FirstOrDefault(x => ReferenceEquals(x.Palette, element.Palette));
			base.PickColor(x, y, priority);
		}
	}

	public override void SaveChanges() {
		try {
			_indexedImage.SaveImage();

			UndoHistory.Clear();
			RedoHistory.Clear();
			NotifyOfPropertyChange(() => CanUndo);
			NotifyOfPropertyChange(() => CanRedo);

			IsModified = false;
			var changeEvent = new ArrangerChangedEvent(_projectArranger, ArrangerChange.Pixels);
			_events.PublishOnUIThread(changeEvent);
		} catch (Exception ex) {
			_ = _windowManager.ShowMessageBox($"Could not save the pixel arranger contents\n{ex.Message}\n{ex.StackTrace}", "Save Error");
		}
	}

	public override void DiscardChanges() {
		_indexedImage.Render();
		UndoHistory.Clear();
		RedoHistory.Clear();
		NotifyOfPropertyChange(() => CanUndo);
		NotifyOfPropertyChange(() => CanRedo);
	}

	public override void SetPixel(int x, int y, byte color) {
		var modelColor = ActivePalette.Colors[color].Color;
		var palColor = new ColorRgba32(modelColor.R, modelColor.G, modelColor.B, modelColor.A);
		var result = _indexedImage.TrySetPixel(x, y, palColor);

		var notifyEvent = result.Match(
			success => {
				if (_activePencilHistory.ModifiedPoints.Add(new Point(x, y))) {
					IsModified = true;
					BitmapAdapter.Invalidate(x, y, 1, 1);
				}

				return new NotifyOperationEvent("");
			},
			fail => new NotifyOperationEvent(fail.Reason)
			);
		_events.PublishOnUIThread(notifyEvent);
	}

	public override byte GetPixel(int x, int y) => _indexedImage.GetPixel(x, y);

	public override void FloodFill(int x, int y, byte fillColor) {
		if (_indexedImage.FloodFill(x, y, fillColor)) {
			AddHistoryAction(new FloodFillAction<byte>(x, y, fillColor));
			IsModified = true;
			Render();
		}
	}

	#endregion

	private MagitekResult ApplyPasteInternal(ArrangerPaste paste) {
		var destX = Math.Max(0, paste.Rect.SnappedLeft);
		var destY = Math.Max(0, paste.Rect.SnappedTop);
		var sourceX = paste.Rect.SnappedLeft >= 0 ? 0 : -paste.Rect.SnappedLeft;
		var sourceY = paste.Rect.SnappedTop >= 0 ? 0 : -paste.Rect.SnappedTop;

		var destStart = new Point(destX, destY);
		var sourceStart = new Point(sourceX, sourceY);

		ArrangerCopy copy;

		if (paste?.Copy is ElementCopy elementCopy) {
			copy = elementCopy.ToPixelCopy();
		} else {
			copy = paste?.Copy;
		}

		if (copy is IndexedPixelCopy indexedCopy) {
			var copyWidth = Math.Min(copy.Width - sourceX, _indexedImage.Width - destX);
			var copyHeight = Math.Min(copy.Height - sourceY, _indexedImage.Height - destY);

			return ImageCopier.CopyPixels(indexedCopy.Image, _indexedImage, sourceStart, destStart, copyWidth, copyHeight,
				PixelRemapOperation.RemapByExactPaletteColors, PixelRemapOperation.RemapByExactIndex);
		} else if (Paste?.Copy is DirectPixelCopy directCopy) {
			throw new NotImplementedException("Direct->Indexed pasting is not yet implemented");
			//var sourceImage = (Paste.OverlayImage as DirectBitmapAdapter).Image;
			//result = ImageCopier.CopyPixels(sourceImage, _indexedImage, sourceStart, destStart, copyWidth, copyHeight,
			//    ImageRemapOperation.RemapByExactPaletteColors, ImageRemapOperation.RemapByExactIndex);
		} else {
			throw new InvalidOperationException($"{nameof(ApplyPaste)} attempted to copy from an arranger of type {paste.Copy.Source.ColorType} to {WorkingArranger.ColorType}");
		}
	}

	public override void ApplyHistoryAction(HistoryAction action) {
		if (action is PencilHistoryAction<byte> pencilAction) {
			foreach (var point in pencilAction.ModifiedPoints) {
				_indexedImage.SetPixel(point.X, point.Y, pencilAction.PencilColor);
			}
		} else if (action is FloodFillAction<byte> floodFillAction) {
			_ = _indexedImage.FloodFill(floodFillAction.X, floodFillAction.Y, floodFillAction.FillColor);
		} else if (action is ColorRemapHistoryAction remapAction) {
			_indexedImage.RemapColors(remapAction.FinalColors.Select(x => (byte)x.Index).ToList());
		} else if (action is PasteArrangerHistoryAction pasteAction) {
			_ = ApplyPasteInternal(pasteAction.Paste);
		}
	}
}
