using TkXamListViewIssue.Models;
using Xamarin.Forms;

namespace TkXamListViewIssue.Controls
{
    public class ItemManagementItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Selected { get; set; }
        public DataTemplate NotSelected { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var i = item as Item;

            return i.IsSelected ? Selected : NotSelected;
        }
    }
}
