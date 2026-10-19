using System.Windows;
using System.Windows.Controls;
using WpfPanel = System.Windows.Controls.Panel;
using WpfRect = System.Windows.Rect;
using WpfSize = System.Windows.Size;

namespace GameRgb;

/// <summary>
/// Uses WPF's measure/arrange pass to display two fluid columns when there is
/// room, and naturally reflows them into one column on narrower windows.
/// </summary>
public sealed class ResponsivePanel : WpfPanel
{
    public double MinColumnWidth { get; set; } = 460;
    public double Gap { get; set; } = 18;

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? Math.Max(MinColumnWidth, Children.Cast<UIElement>().Select(x => x.DesiredSize.Width).DefaultIfEmpty().Max())
            : availableSize.Width;
        var twoColumns = Children.Count > 1 && width >= MinColumnWidth * 2 + Gap;
        var childWidth = twoColumns ? (width - Gap) / 2 : width;

        foreach (UIElement child in Children)
            child.Measure(new WpfSize(childWidth, double.PositiveInfinity));

        var height = twoColumns
            ? Children.Cast<UIElement>().Select(x => x.DesiredSize.Height).DefaultIfEmpty().Max()
            : Children.Cast<UIElement>().Sum(x => x.DesiredSize.Height) + Math.Max(0, Children.Count - 1) * Gap;
        return new WpfSize(width, height);
    }

    protected override WpfSize ArrangeOverride(WpfSize finalSize)
    {
        var twoColumns = Children.Count > 1 && finalSize.Width >= MinColumnWidth * 2 + Gap;
        if (twoColumns)
        {
            var width = (finalSize.Width - Gap) / 2;
            for (var i = 0; i < Children.Count; i++)
                Children[i].Arrange(new WpfRect(i * (width + Gap), 0, width, Children[i].DesiredSize.Height));
        }
        else
        {
            var y = 0d;
            foreach (UIElement child in Children)
            {
                child.Arrange(new WpfRect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height + Gap;
            }
        }
        return finalSize;
    }
}
