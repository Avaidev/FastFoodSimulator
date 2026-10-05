using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace FastFoodSimulator.Behaviors;

public static class AutoScrollBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled",
        typeof(bool),
        typeof(AutoScrollBehavior),
        new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        var items = (INotifyCollectionChanged)listBox.Items;

        void Handler(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add && listBox.Items.Count > 0)
            {
                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
            }
        }

        if ((bool)e.NewValue)
        {
            items.CollectionChanged += Handler;
        }
        else
        {
            items.CollectionChanged -= Handler;
        }
    }
}
