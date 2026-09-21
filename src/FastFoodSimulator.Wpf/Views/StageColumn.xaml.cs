using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastFoodSimulator.Wpf.Views;

/// <summary>One column of the restaurant map: a titled area filled with number chips.</summary>
public partial class StageColumn : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(StageColumn), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(StageColumn), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActiveGlyphProperty =
        DependencyProperty.Register(nameof(ActiveGlyph), typeof(string), typeof(StageColumn), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(StageColumn), new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty TintProperty =
        DependencyProperty.Register(nameof(Tint), typeof(Brush), typeof(StageColumn), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(StageColumn), new PropertyMetadata(null));

    public StageColumn() => InitializeComponent();

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string ActiveGlyph
    {
        get => (string)GetValue(ActiveGlyphProperty);
        set => SetValue(ActiveGlyphProperty, value);
    }

    public Brush Accent
    {
        get => (Brush)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public Brush Tint
    {
        get => (Brush)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
