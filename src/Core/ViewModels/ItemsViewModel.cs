using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Telerik.XamarinForms.Common;
using Telerik.XamarinForms.DataControls.ListView;
using Telerik.XamarinForms.DataControls.ListView.Commands;
using TkXamListViewIssue.Models;

namespace TkXamListViewIssue.ViewModels
{
    public sealed class ItemsViewModel : IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly ReadOnlyObservableCollection<Item> _selectedItems;

        public ItemsViewModel()
        {
            var itemsLoader = App.Current.DataSource.Items
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<Item>.Ascending(x => x.DisplayOrder))
                .Do(_ => System.Diagnostics.Debug.WriteLine("***DATA_SOURCE_CHANGE***"))
                .Publish();

            var allItemsLoader = itemsLoader
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(Items, new TestObservableCollectionAdapter()) // force reset when item changes ("refresh" change type)
                //.Bind(Items) // the default implementation doesn't work
                .Subscribe();

            var selectedItemsLoader = itemsLoader
                .Filter(x => x.IsSelected)
                .Sort(SortExpressionComparer<Item>.Ascending(x => x.DisplayOrder.Value))
                .Bind(out _selectedItems)
                .Subscribe();

            BeginReorder = ReactiveCommand.Create<ReorderStartingCommandContext, Unit>(BeginReorderImpl);
            CommitReorder = ReactiveCommand.Create<ReorderEndedCommandContext, Unit>(CommitReorderImpl);
            ItemTapped = ReactiveCommand.Create<ItemTapCommandContext, Unit>(ItemTappedImpl);

            _disposables = new CompositeDisposable(itemsLoader.Connect(), allItemsLoader, selectedItemsLoader);
        }

        public ReactiveCommand<ReorderStartingCommandContext, Unit> BeginReorder { get; }

        public ReactiveCommand<ReorderEndedCommandContext, Unit> CommitReorder { get; }

        public ReactiveCommand<ItemTapCommandContext, Unit> ItemTapped { get; }

        public IObservableCollection<Item> Items { get; } = new ObservableCollectionExtended<Item>();

        public ObservableCollection<GroupDescriptorBase> GroupDescriptors { get; } = new ObservableCollectionExtended<GroupDescriptorBase>(new[] { GroupDescriptor });

        public ObservableCollection<SortDescriptorBase> SortDescriptors { get; } = new ObservableCollectionExtended<SortDescriptorBase>(new[] { SortDescriptor });

        private Unit BeginReorderImpl(ReorderStartingCommandContext context)
        {
            var item = (Item)context.Item;

            // don't allow reordering of unselected items
            if (!item.IsSelected)
            {
                context.Cancel = true;
            }

            return Unit.Default;
        }

        private Unit CommitReorderImpl(ReorderEndedCommandContext context)
        {
            if ((string)context.DestinationGroup.Key != PinnedGroupKey)
            {
                return Unit.Default;
            }

            var selectedItems = _selectedItems.ToList();
            var item = (Item)context.Item;
            var index = selectedItems.IndexOf(item);
            var destItem = (Item)context.DestinationItem;
            var destIndex = selectedItems.IndexOf(destItem);

            using (Items.SuspendNotifications())
            {
                item.DisplayOrder = destIndex;

                if (index > destIndex)
                {
                    // item was moved up
                    for (var i = destIndex; i < index; i++)
                    {
                        var itemToAdjust = selectedItems[i];
                        itemToAdjust.DisplayOrder = i + 1;
                    }
                }
                else if (index < destIndex)
                {
                    // item was moved down
                    for (var i = index + 1; i <= destIndex; i++)
                    {
                        var itemToAdjust = selectedItems[i];
                        itemToAdjust.DisplayOrder = i - 1;
                    }
                } 
            }
            
            System.Diagnostics.Debug.WriteLine($"ITEM_REORDERED: Id={item.Id},IsSelected={item.IsSelected},DisplayOrder={item.DisplayOrder}");

            LogAllItems();

            return Unit.Default;
        }

        private void UnselectItem(int index, Item unselectedItem)
        {
            var selectedItems = _selectedItems.ToList();
            selectedItems.Remove(unselectedItem);

            using (Items.SuspendNotifications())
            {
                unselectedItem.DisplayOrder = null;
                unselectedItem.IsSelected = false;

                for (var i = index; i < selectedItems.Count; i++)
                {
                    var itemToAdjust = selectedItems[i];
                    itemToAdjust.DisplayOrder = i;
                }
            }
        }

        private static readonly SortDescriptorBase SortDescriptor = new DelegateSortDescriptor
        {
            Comparer = ShortcutManagerListViewItemSortComparer
        };

        private static readonly GroupDescriptorBase GroupDescriptor = new DelegateGroupDescriptor
        {
            KeyExtractor = ShortcutManagerListViewGroupKeyExtractor,
            SortOrder = SortOrder.Descending
        };

        private static int ShortcutManagerListViewItemSortComparer(object arg1, object arg2)
        {
            var item1 = (Item)arg1;
            var item2 = (Item)arg2;

            if (item1.IsSelected && item2.IsSelected)
            {
                return item1.DisplayOrder.Value.CompareTo(item2.DisplayOrder.Value);
            }

            return item1.Name.CompareTo(item2.Name);
        }

        private static object ShortcutManagerListViewGroupKeyExtractor(object arg)
        {
            var item = (Item)arg;
            return item.IsSelected ? PinnedGroupKey : string.Empty;
        }

        private const string PinnedGroupKey = "SELECTED";

        private Unit ItemTappedImpl(ItemTapCommandContext context)
        {
            var item = (Item)context.Item;
            var index = Items.IndexOf(item);

            using (item.DelayChangeNotifications())
            {
                if (item.IsSelected)
                {
                    UnselectItem(index, item);
                }
                else
                {
                    SelectItem(item);
                }

                System.Diagnostics.Debug.WriteLine($"ITEM_TAPPED: Id={item.Id},IsSelected={item.IsSelected},DisplayOrder={item.DisplayOrder}");
            }

            LogAllItems();

            return Unit.Default;
        }

        private void SelectItem(Item item)
        {
            // order matters here
            item.DisplayOrder = Items.Count(x => x.IsSelected);
            item.IsSelected = true;
        }

        private void LogAllItems()
        {
            System.Diagnostics.Debug.WriteLine($"----------");
            foreach (var item in Items)
            {
                System.Diagnostics.Debug.WriteLine($"Id={item.Id},IsSelected={item.IsSelected},DisplayOrder={item.DisplayOrder}");
            }
            System.Diagnostics.Debug.WriteLine($"----------");
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }

    internal class TestObservableCollectionAdapter : IObservableCollectionAdaptor<Item, int>
    {
        private readonly IObservableCollectionAdaptor<Item, int> _defaultAdapter = new ObservableCollectionAdaptor<Item, int>();

        public void Adapt(IChangeSet<Item, int> changes, IObservableCollection<Item> collection)
        {
            using (collection.SuspendNotifications())
            {
                //foreach (var change in changes)
                //{
                //    if (change.Reason == ChangeReason.Refresh)
                //    {
                //        collection.Replace(change.Current, change.Current);
                //    }
                //}
                
                _defaultAdapter.Adapt(changes, collection);
            }
        }
    }
}
