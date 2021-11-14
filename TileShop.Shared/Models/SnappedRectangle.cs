﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TileShop.Shared.Models;

public enum SnapMode { Element, Pixel }
public enum ElementSnapRounding { Floor, Ceiling, Collapse, Expand }
public class SnappedRectangle : INotifyPropertyChanged {
	private SnapMode _snapMode;
	public SnapMode SnapMode {
		get => _snapMode;
		set {
			SetField(ref _snapMode, value);
			Snap();
		}
	}

	private Size _maximumSize;
	public Size MaximumSize {
		get => _maximumSize;
		set {
			SetField(ref _maximumSize, value);
			Snap();
		}
	}

	private Size _elementSize;
	public Size ElementSize {
		get => _elementSize;
		set {
			SetField(ref _maximumSize, value);
			Snap();
		}
	}

	private double _left;
	public double Left {
		get => _left;
		set {
			SetField(ref _left, value);
			Snap();
		}
	}

	private double _right;
	public double Right {
		get => _right;
		set {
			SetField(ref _right, value);
			Snap();
		}
	}

	private double _top;
	public double Top {
		get => _top;
		set {
			SetField(ref _top, value);
			Snap();
		}
	}

	private double _bottom;
	public double Bottom {
		get => _bottom;
		set {
			SetField(ref _bottom, value);
			Snap();
		}
	}

	private int _snappedLeft;
	public int SnappedLeft {
		get => _snappedLeft;
		private set => SetField(ref _snappedLeft, value);
	}

	private int _snappedRight;
	public int SnappedRight {
		get => _snappedRight;
		private set => SetField(ref _snappedRight, value);
	}

	private int _snappedTop;
	public int SnappedTop {
		get => _snappedTop;
		private set => SetField(ref _snappedTop, value);
	}

	private int _snappedBottom;
	public int SnappedBottom {
		get => _snappedBottom;
		private set => SetField(ref _snappedBottom, value);
	}

	private int _snappedWidth;
	public int SnappedWidth {
		get => _snappedWidth;
		private set => SetField(ref _snappedWidth, value);
	}

	private int _snappedHeight;
	public int SnappedHeight {
		get => _snappedHeight;
		private set => SetField(ref _snappedHeight, value);
	}

	public ElementSnapRounding SnapRounding { get; set; }

	public SnappedRectangle() : this(new Size(int.MaxValue, int.MaxValue), new Size(1, 1), SnapMode.Pixel) { }

	public SnappedRectangle(Size maximumSize, Size elementSize, SnapMode snapMode, ElementSnapRounding snapRounding = ElementSnapRounding.Floor) {
		_maximumSize = maximumSize;
		_elementSize = elementSize;
		SnapMode = snapMode;
		SnapRounding = snapRounding;
	}

	public void SetBounds(double left, double right, double top, double bottom) {
		_left = left;
		_right = right;
		_top = top;
		_bottom = bottom;
		OnPropertyChanged(nameof(Left));
		OnPropertyChanged(nameof(Right));
		OnPropertyChanged(nameof(Top));
		OnPropertyChanged(nameof(Bottom));
		Snap();
	}

	/// <summary>
	/// Translates the upper left corner of the rect to the specified coordinate, maintaining the rect size
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void MoveTo(double x, double y) {
		var width = Math.Abs(Right - Left);
		var height = Math.Abs(Bottom - Top);
		Left = x;
		Top = y;
		Right = Left + width;
		Bottom = Top + height;
	}

	/// <summary>
	/// Sets the begin point to the specified coordinate, modifying the rect size
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void SetBeginPoint(double x, double y) {
		_left = x;
		_top = y;

		Snap();
		OnPropertyChanged(nameof(Left));
		OnPropertyChanged(nameof(Top));
	}

	/// <summary>
	/// Sets the end point to the specified coordinate, modifying the rect size
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public void SetEndpoint(double x, double y) {
		_right = x;
		_bottom = y;

		Snap();
		OnPropertyChanged(nameof(Right));
		OnPropertyChanged(nameof(Bottom));
	}

	/// <summary>
	/// Checks if the specified point is within the unsnapped bounds of the rectangle
	/// </summary>
	/// <returns>True if within snapped bounds</returns>
	public bool ContainsPointUnsnapped(double x, double y) =>
		(x >= Left) && (x <= Right) && (y >= Top) && (y <= Bottom);

	/// <summary>
	/// Checks if the specified point is within the snapped bounds of the rectangle
	/// </summary>
	/// <returns>True if within snapped bounds</returns>
	public bool ContainsPointSnapped(double x, double y) =>
		(x >= SnappedLeft) && (x <= SnappedRight) && (y >= SnappedTop) && (y <= SnappedBottom);

	private void Snap() {
		if (SnapMode == SnapMode.Element) {
			SnapElements();
		} else if (SnapMode == SnapMode.Pixel) {
			SnapPixels();
		}
	}

	private void SnapElements() {
		if (SnapRounding == ElementSnapRounding.Floor) {
			SnappedLeft = (int)(Math.Floor(Math.Min(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedRight = (int)(Math.Floor(Math.Max(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedTop = (int)(Math.Floor(Math.Min(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
			SnappedBottom = (int)(Math.Floor(Math.Max(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
		} else if (SnapRounding == ElementSnapRounding.Ceiling) {
			SnappedLeft = (int)(Math.Ceiling(Math.Min(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedRight = (int)(Math.Ceiling(Math.Max(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedTop = (int)(Math.Ceiling(Math.Min(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
			SnappedBottom = (int)(Math.Ceiling(Math.Max(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
		}

		if (SnapRounding == ElementSnapRounding.Collapse) {
			SnappedLeft = (int)(Math.Ceiling(Math.Min(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedRight = (int)(Math.Floor(Math.Max(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedTop = (int)(Math.Ceiling(Math.Min(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
			SnappedBottom = (int)(Math.Floor(Math.Max(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
		} else if (SnapRounding == ElementSnapRounding.Expand) {
			SnappedLeft = (int)(Math.Floor(Math.Min(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedRight = (int)(Math.Ceiling(Math.Max(Left, Right) / _elementSize.Width) * _elementSize.Width);
			SnappedTop = (int)(Math.Floor(Math.Min(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
			SnappedBottom = (int)(Math.Ceiling(Math.Max(Top, Bottom) / _elementSize.Height) * _elementSize.Height);
		}

		SnappedWidth = SnappedRight - SnappedLeft;
		SnappedHeight = SnappedBottom - SnappedTop;
	}

	private void SnapPixels() {
		SnappedLeft = (int)Math.Round(Math.Min(Left, Right));
		SnappedRight = (int)Math.Round(Math.Max(Left, Right));
		SnappedTop = (int)Math.Round(Math.Min(Top, Bottom));
		SnappedBottom = (int)Math.Round(Math.Max(Top, Bottom));
		SnappedWidth = SnappedRight - SnappedLeft;
		SnappedHeight = SnappedBottom - SnappedTop;
	}

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
		if (EqualityComparer<T>.Default.Equals(field, value)) {
			return false;
		}

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
