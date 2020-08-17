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

        public ItemsViewModel()
        {
            var itemsLoader = App.Current.DataSource.Items
                .Connect()
                .AutoRefresh()
                .Do(_ => System.Diagnostics.Debug.WriteLine("***DATA_SOURCE_CHANGE***"))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(Items)
                .Subscribe();

            BeginReorder = ReactiveCommand.Create<ReorderStartingCommandContext, Unit>(BeginReorderImpl);
            CommitReorder = ReactiveCommand.Create<ReorderEndedCommandContext, Unit>(CommitReorderImpl);
            ItemTapped = ReactiveCommand.Create<ItemTapCommandContext, Unit>(ItemTappedImpl);

            _disposables = new CompositeDisposable(itemsLoader);
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

            var item = (Item)context.Item;
            var destItem = (Item)context.DestinationItem;
            var destIndex = Items.IndexOf(destItem);

            if (context.Placement == ItemReorderPlacement.After)
            {
                destIndex++;
            }

            item.DisplayOrder = destIndex;

            System.Diagnostics.Debug.WriteLine($"ITEM_REORDERED: Id={item.Id},IsSelected={item.IsSelected},DisplayOrder={item.DisplayOrder}");

            foreach (var otherItem in Items.Where(x => x.IsSelected && x.DisplayOrder.Value >= destIndex && x != item))
            {
                otherItem.DisplayOrder = otherItem.DisplayOrder.Value + 1;
            }

            return Unit.Default;
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

            using (item.DelayChangeNotifications())
            {
                item.DisplayOrder = item.IsSelected ? (int?)null : Items.Count(x => x.IsSelected);
                item.IsSelected = !item.IsSelected;

                // this results in an invalid state
                //Items.Remove(item);
                //Items.Add(item);

                System.Diagnostics.Debug.WriteLine($"ITEM_TAPPED: Id={item.Id},IsSelected={item.IsSelected},DisplayOrder={item.DisplayOrder}");
            }

            return Unit.Default;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
