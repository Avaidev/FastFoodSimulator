using System.Collections.ObjectModel;

namespace FastFoodSimulator.Wpf.ViewModels;

/// <summary>
/// Applies a new desired list to an ObservableCollection with the minimum of changes,
/// so that only newly arrived chips are created (and animated) instead of rebuilding the whole list.
/// </summary>
public static class ChipCollectionSynchronizer
{
    public static void Sync(ObservableCollection<ChipViewModel> target, IReadOnlyList<(int Key, bool IsActive)> desired)
    {
        var desiredKeys = desired.Select(item => item.Key).ToHashSet();

        for (var i = target.Count - 1; i >= 0; i--)
        {
            if (!desiredKeys.Contains(target[i].Key))
            {
                target.RemoveAt(i);
            }
        }

        for (var i = 0; i < desired.Count; i++)
        {
            var (key, isActive) = desired[i];

            if (i < target.Count && target[i].Key == key)
            {
                target[i].IsActive = isActive;
                continue;
            }

            var existingIndex = IndexOf(target, key, i + 1);
            if (existingIndex >= 0)
            {
                target.Move(existingIndex, i);
                target[i].IsActive = isActive;
            }
            else
            {
                target.Insert(i, new ChipViewModel(key) { IsActive = isActive });
            }
        }
    }

    private static int IndexOf(ObservableCollection<ChipViewModel> collection, int key, int startIndex)
    {
        for (var i = startIndex; i < collection.Count; i++)
        {
            if (collection[i].Key == key)
            {
                return i;
            }
        }

        return -1;
    }
}
