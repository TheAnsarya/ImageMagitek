﻿using System;
using System.Linq;
using Stylet;
using ImageMagitek;
using ImageMagitek.Services;
using TileShop.Shared.EventModels;
using TileShop.Shared.Models;
using TileShop.WPF.Behaviors;
using TileShop.WPF.Imaging;
using TileShop.WPF.EventModels;
using Jot;
using TileShop.WPF.Models;

namespace TileShop.WPF.ViewModels
{
    public class SequentialArrangerEditorViewModel : ArrangerEditorViewModel, IMouseCaptureProxy
    {
        private readonly ICodecService _codecService;
        private readonly Tracker _tracker;
        private FileBitAddress _address;

        private BindableCollection<string> _codecNames = new BindableCollection<string>();
        public BindableCollection<string> CodecNames
        {
            get => _codecNames;
            set => SetAndNotify(ref _codecNames, value);
        }

        private string _selectedCodecName;
        public string SelectedCodecName
        {
            get => _selectedCodecName;
            set
            {
                SetAndNotify(ref _selectedCodecName, value);
                ChangeCodec();
            }
        }

        private BindableCollection<PaletteModel> _palettes = new BindableCollection<PaletteModel>();
        public BindableCollection<PaletteModel> Palettes
        {
            get => _palettes;
            set => SetAndNotify(ref _palettes, value);
        }

        private PaletteModel _selectedPalette;
        public PaletteModel SelectedPalette
        {
            get => _selectedPalette;
            set
            {
                SetAndNotify(ref _selectedPalette, value);
                ChangePalette(SelectedPalette);
            }
        }

        private int _tiledElementWidth = 8;
        public int TiledElementWidth
        {
            get => _tiledElementWidth;
            set
            {
                var preferredWidth = (_workingArranger as SequentialArranger).ActiveCodec.GetPreferredWidth(value);
                SetAndNotify(ref _tiledElementWidth, preferredWidth);
                ChangeCodecDimensions(TiledElementWidth, TiledElementHeight);
            }
        }

        private int _tiledElementHeight = 8;
        public int TiledElementHeight
        {
            get => _tiledElementHeight;
            set
            {
                var preferredHeight = (_workingArranger as SequentialArranger).ActiveCodec.GetPreferredHeight(value);
                SetAndNotify(ref _tiledElementHeight, preferredHeight);
                ChangeCodecDimensions(TiledElementWidth, TiledElementHeight);
            }
        }

        private int _tiledArrangerWidth = 8;
        public int TiledArrangerWidth
        {
            get => _tiledArrangerWidth;
            set
            {
                SetAndNotify(ref _tiledArrangerWidth, value);
                ResizeArranger(TiledArrangerWidth, TiledArrangerHeight);
            }
        }

        private int _tiledArrangerHeight = 16;
        public int TiledArrangerHeight
        {
            get => _tiledArrangerHeight;
            set
            {
                SetAndNotify(ref _tiledArrangerHeight, value);
                ResizeArranger(TiledArrangerWidth, TiledArrangerHeight);
            }
        }

        private int _linearArrangerWidth = 256;
        public int LinearArrangerWidth
        {
            get => _linearArrangerWidth;
            set
            {
                var preferredWidth = (_workingArranger as SequentialArranger).ActiveCodec.GetPreferredWidth(value);
                SetAndNotify(ref _linearArrangerWidth, preferredWidth);
                ChangeCodecDimensions(LinearArrangerWidth, LinearArrangerHeight);
            }
        }

        private int _linearArrangerHeight = 256;
        public int LinearArrangerHeight
        {
            get => _linearArrangerHeight;
            set
            {
                var preferredHeight = (_workingArranger as SequentialArranger).ActiveCodec.GetPreferredHeight(value);
                SetAndNotify(ref _linearArrangerHeight, preferredHeight);
                ChangeCodecDimensions(LinearArrangerWidth, LinearArrangerHeight);
            }
        }

        private bool _canResize;
        public bool CanResize
        {
            get => _canResize;
            set => SetAndNotify(ref _canResize, value);
        }

        private int _widthIncrement = 1;
        public int WidthIncrement
        {
            get => _widthIncrement;
            set => SetAndNotify(ref _widthIncrement, value);
        }

        private int _heightIncrement = 1;
        public int HeightIncrement
        {
            get => _heightIncrement;
            set => SetAndNotify(ref _heightIncrement, value);
        }

        public SequentialArrangerEditorViewModel(SequentialArranger arranger, IEventAggregator events, IWindowManager windowManager, 
            Tracker tracker, ICodecService codecService, IPaletteService paletteService) :
            base(events, windowManager, paletteService)
        {
            Resource = arranger;
            _workingArranger = arranger;
            _codecService = codecService;
            _tracker = tracker;
            DisplayName = Resource?.Name ?? "Unnamed Arranger";

            if (_workingArranger.ColorType == PixelColorType.Indexed)
            {
                _indexedImage = new IndexedImage(_workingArranger);
                BitmapAdapter = new IndexedBitmapAdapter(_indexedImage);
            }
            else if (_workingArranger.ColorType == PixelColorType.Direct)
            {
                _directImage = new DirectImage(_workingArranger);
            }

            foreach (var name in codecService.GetSupportedCodecNames().OrderBy(x => x))
                CodecNames.Add(name);
            _selectedCodecName = arranger.ActiveCodec.Name;

            if (arranger.Layout == ArrangerLayout.Tiled)
            {
                _tiledElementWidth = arranger.ElementPixelSize.Width;
                _tiledElementHeight = arranger.ElementPixelSize.Height;
                _tiledArrangerHeight = arranger.ArrangerElementSize.Height;
                _tiledArrangerWidth = arranger.ArrangerElementSize.Width;
                SnapMode = SnapMode.Element;
            }
            else if(arranger.Layout == ArrangerLayout.Single)
            {
                _linearArrangerHeight = arranger.ArrangerPixelSize.Height;
                _linearArrangerWidth = arranger.ArrangerPixelSize.Width;
                SnapMode = SnapMode.Pixel;
            }

            CanResize = arranger.ActiveCodec.CanResize;
            WidthIncrement = arranger.ActiveCodec.WidthResizeIncrement;
            HeightIncrement = arranger.ActiveCodec.HeightResizeIncrement;

            Palettes = new BindableCollection<PaletteModel>(_paletteService.GlobalPalettes.Select(x => new PaletteModel(x)));
            SelectedPalette = Palettes.First();

            CreateGridlines();
        }

        public override void SaveChanges()
        {
            IsModified = false;
        }

        public override void DiscardChanges()
        {
            IsModified = false;
        }

        public void MoveByteDown() => Move(ArrangerMoveType.ByteDown);
        public void MoveByteUp() => Move(ArrangerMoveType.ByteUp);
        public void MoveRowDown() => Move(ArrangerMoveType.RowDown);
        public void MoveRowUp() => Move(ArrangerMoveType.RowUp);
        public void MoveColumnRight() => Move(ArrangerMoveType.ColRight);
        public void MoveColumnLeft() => Move(ArrangerMoveType.ColLeft);
        public void MovePageDown() => Move(ArrangerMoveType.PageDown);
        public void MovePageUp() => Move(ArrangerMoveType.PageUp);
        public void MoveHome() => Move(ArrangerMoveType.Home);
        public void MoveEnd() => Move(ArrangerMoveType.End);

        public void ExpandWidth()
        {
            if (IsTiledLayout)
                TiledArrangerWidth++;
            else
                LinearArrangerWidth += WidthIncrement;
        }

        public void ExpandHeight()
        {
            if (IsTiledLayout)
                TiledArrangerHeight++;
            else
                LinearArrangerHeight += HeightIncrement;
        }

        public void ShrinkWidth()
        {
            if (IsTiledLayout)
                TiledArrangerWidth = Math.Clamp(TiledArrangerWidth - 1, 1, int.MaxValue);
            else
                LinearArrangerHeight = Math.Clamp(LinearArrangerHeight - WidthIncrement, WidthIncrement, int.MaxValue);
        }

        public void ShrinkHeight()
        {
            if (IsTiledLayout)
                TiledArrangerHeight = Math.Clamp(TiledArrangerHeight - 1, 1, int.MaxValue);
            else
                LinearArrangerWidth = Math.Clamp(LinearArrangerWidth - HeightIncrement, HeightIncrement, int.MaxValue);
        }

        public void JumpToOffset()
        {
            var model = new JumpToOffsetViewModel();
            _tracker.Track(model);
            var result = _windowManager.ShowDialog(model);

            if (result is true)
            {
                Move(model.Result * 8);
                _tracker.Persist(model);
            }
        }

        public void NewScatteredArrangerFromSelection()
        {
            if (SnapMode == SnapMode.Element)
            {
                int x = Overlay.SelectionRect.SnappedLeft / _workingArranger.ElementPixelSize.Width;
                int y = Overlay.SelectionRect.SnappedTop / _workingArranger.ElementPixelSize.Height;
                int width = Overlay.SelectionRect.SnappedWidth / _workingArranger.ElementPixelSize.Width;
                int height = Overlay.SelectionRect.SnappedHeight / _workingArranger.ElementPixelSize.Height;
                var model = new AddScatteredArrangerFromExistingEvent(_workingArranger, x, y, width, height);
                _events.PublishOnUIThread(model);
            }
            else
            {
                _windowManager.ShowMessageBox("Selection must be performed in element snap mode to create a new Scattered Arranger", "Error");
            }
        }

        public void NewScatteredArrangerFromImage()
        {
            var model = new AddScatteredArrangerFromExistingEvent(_workingArranger, 0, 0, 1, 1);
            _events.PublishOnUIThread(model);
        }

        private void Move(ArrangerMoveType moveType)
        {
            var oldAddress = (_workingArranger as SequentialArranger).GetInitialSequentialFileAddress();
            _address = (_workingArranger as SequentialArranger).Move(moveType);

            if (oldAddress != _address)
                Render();

            string notifyMessage = $"File Offset: 0x{_address.FileOffset:X}";
            var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
            _events.PublishOnUIThread(notifyEvent);
        }

        private void Move(long offset)
        {
            var oldAddress = (_workingArranger as SequentialArranger).GetInitialSequentialFileAddress();
            _address = (_workingArranger as SequentialArranger).Move(offset);

            if (oldAddress != _address)
                Render();

            string notifyMessage = $"File Offset: 0x{_address.FileOffset:X}";
            var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
            _events.PublishOnUIThread(notifyEvent);
        }

        private void ResizeArranger(int arrangerWidth, int arrangerHeight)
        {
            if (arrangerWidth <= 0 || arrangerHeight <= 0)
                return;

            if (arrangerWidth == _workingArranger.ArrangerElementSize.Width && 
                arrangerHeight == _workingArranger.ArrangerElementSize.Height && IsTiledLayout)
                return;

            if (arrangerWidth == _workingArranger.ArrangerPixelSize.Width &&
                arrangerHeight == _workingArranger.ArrangerPixelSize.Height && IsSingleLayout)
                return;

            (_workingArranger as SequentialArranger).Resize(arrangerWidth, arrangerHeight);
            CreateGridlines();
            Render();
        }

        public void SelectNextCodec()
        {
            var index = CodecNames.IndexOf(SelectedCodecName) + 1;
            SelectedCodecName = CodecNames[index % CodecNames.Count];
        }

        public void SelectPreviousCodec()
        {
            var index = CodecNames.IndexOf(SelectedCodecName) - 1;
            if (index < 0)
                index = CodecNames.Count - 1;
            SelectedCodecName = CodecNames[index];
        }

        private void ChangeCodec()
        {
            var codec = _codecService.CodecFactory.GetCodec(SelectedCodecName);
            if (codec.Layout == ImageMagitek.Codec.ImageLayout.Tiled)
            {
                _tiledElementHeight = codec.Height;
                _tiledElementWidth = codec.Width;

                (_workingArranger as SequentialArranger).ChangeCodec(codec, TiledArrangerWidth, TiledArrangerHeight);
                SnapMode = SnapMode.Element;

                NotifyOfPropertyChange(() => TiledElementHeight);
                NotifyOfPropertyChange(() => TiledElementWidth);
            }
            else if (codec.Layout == ImageMagitek.Codec.ImageLayout.Single)
            {
                ShowGridlines = false;
                _linearArrangerHeight = codec.Height;
                _linearArrangerWidth = codec.Width;

                (_workingArranger as SequentialArranger).ChangeCodec(codec, 1, 1);
                SnapMode = SnapMode.Pixel;
                NotifyOfPropertyChange(() => LinearArrangerHeight);
                NotifyOfPropertyChange(() => LinearArrangerWidth);
            }

            _address = (_workingArranger as SequentialArranger).FileAddress;
            CanResize = codec.CanResize;
            WidthIncrement = codec.WidthResizeIncrement;
            HeightIncrement = codec.HeightResizeIncrement;
            CreateGridlines();
            Render();

            NotifyOfPropertyChange(() => IsTiledLayout);
            NotifyOfPropertyChange(() => IsSingleLayout);
            NotifyOfPropertyChange(() => CanShowGridlines);

            string notifyMessage = $"File Offset: 0x{_address.FileOffset:X}";
            var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
            _events.PublishOnUIThread(notifyEvent);
        }

        private void ChangePalette(PaletteModel pal)
        {
            (_workingArranger as SequentialArranger).ChangePalette(pal.Palette);
            Render();
        }

        private void ChangeCodecDimensions(int width, int height)
        {
            var codec = _codecService.CodecFactory.GetCodec(SelectedCodecName, width, height);
            (_workingArranger as SequentialArranger).ChangeCodec(codec);
            CreateGridlines();
            Render();
        }

        protected override void Render()
        {
            CancelOverlay();

            if (_workingArranger.ColorType == PixelColorType.Indexed)
            {
                _indexedImage.Render();
                BitmapAdapter.Invalidate();
            }
            else if (_workingArranger.ColorType == PixelColorType.Direct)
            {
                _directImage.Render();
                //ArrangerSource = new DirectImageSource(_directImage);
            }
        }

        public override void OnMouseMove(object sender, MouseCaptureArgs e)
        {
            if (Overlay.State == OverlayState.Selecting)
                Overlay.UpdateSelectionEndPoint(e.X / Zoom, e.Y / Zoom);

            if (Overlay.State == OverlayState.Selecting || Overlay.State == OverlayState.Selected)
            {
                string notifyMessage;
                var rect = Overlay.SelectionRect;
                if (rect.SnapMode == SnapMode.Element)
                    notifyMessage = $"Element Selection: {rect.SnappedWidth / _workingArranger.ElementPixelSize.Width} x {rect.SnappedHeight / _workingArranger.ElementPixelSize.Height}" +
                        $" at ({rect.SnappedLeft / _workingArranger.ElementPixelSize.Width}, {rect.SnappedRight / _workingArranger.ElementPixelSize.Height})";
                else
                    notifyMessage = $"Pixel Selection: {rect.SnappedWidth} x {rect.SnappedHeight}" +
                        $" at ({rect.SnappedLeft} x {rect.SnappedTop})";
                var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
                _events.PublishOnUIThread(notifyEvent);
            }
            else
            {
                string notifyMessage = $"File Offset: 0x{_address.FileOffset:X} ({(int)Math.Round(e.X / Zoom)}, {(int)Math.Round(e.Y / Zoom)})";
                var notifyEvent = new NotifyStatusEvent(notifyMessage, NotifyStatusDuration.Indefinite);
                _events.PublishOnUIThread(notifyEvent);
            }
        }

        /// <summary>
        /// Checks if the specified arranger can be copied into the current SequentialArranger
        /// SequentialArrangers can only copy pixels
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected override bool CanAcceptTransfer(ArrangerTransferModel model)
        {
            CanPastePixels = true;
            CanPasteElements = false;
            return true;
        }
    }
}
