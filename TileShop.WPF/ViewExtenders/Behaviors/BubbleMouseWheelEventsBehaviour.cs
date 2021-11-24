using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace TileShop.WPF.Behaviors;

internal class BubbleMouseWheelEventsBehaviour : Behavior<UIElement> {
	protected override void OnAttached() {
		base.OnAttached();
		AssociatedObject.PreviewMouseWheel += PreviewMouseWheel;
	}

	protected override void OnDetaching() {
		AssociatedObject.PreviewMouseWheel -= PreviewMouseWheel;
		base.OnDetaching();
	}

	private void PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
		if (e.Delta == 0) {
			return;
		}

		var scrollViewer = AssociatedObject.GetVisualDescendant<ScrollViewer>();
		var scrollPos = scrollViewer.ContentVerticalOffset;
		if ((scrollPos == scrollViewer.ScrollableHeight && e.Delta < 0) || (scrollPos == 0 && e.Delta > 0)) {
			var rerouteTo = AssociatedObject;
			if (ReferenceEquals(scrollViewer, AssociatedObject)) {
				rerouteTo = (UIElement)VisualTreeHelper.GetParent(AssociatedObject);
			}

			e.Handled = true;
			var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) {
				RoutedEvent = UIElement.MouseWheelEvent
			};
			rerouteTo.RaiseEvent(e2);
		}
	}
}
