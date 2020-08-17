using DynamicData;
using TkXamListViewIssue.Models;

namespace TkXamListViewIssue
{
    public sealed class AppDataSource
    {
        public AppDataSource()
        {
            // init items
            Items = new SourceCache<Item, int>(x => x.Id);

            Items.AddOrUpdate(new Item(1, "Item 1", true, 0));
            Items.AddOrUpdate(new Item(2, "Item 2", true, 1));
            Items.AddOrUpdate(new Item(3, "Item 3", false, null));
            Items.AddOrUpdate(new Item(4, "Item 4", false, null));
        }

        public SourceCache<Item, int> Items { get; }
    }
}
