using ReactiveUI;
using ReactiveUI.XamForms;
using System.Reactive.Disposables;
using TkXamListViewIssue.ViewModels;

namespace TkXamListViewIssue.Views
{
    public partial class ItemsPage : ReactiveContentPage<ItemsViewModel>
    {
        public ItemsPage()
        {
            InitializeComponent();

            ViewModel = new ItemsViewModel();

            this.WhenActivated(disposables =>
            {
                this
                    .OneWayBind(ViewModel, vm => vm.Items, v => v.ItemManagerListView.ItemsSource)
                    .DisposeWith(disposables);

                this
                    .OneWayBind(ViewModel, vm => vm.GroupDescriptors, v => v.ItemManagerListView.GroupDescriptors)
                    .DisposeWith(disposables);

                this
                    .OneWayBind(ViewModel, vm => vm.SortDescriptors, v => v.ItemManagerListView.SortDescriptors)
                    .DisposeWith(disposables);

                this
                    .OneWayBind(ViewModel, vm => vm.BeginReorder, v => v.BeginReorder.Command)
                    .DisposeWith(disposables);

                this
                    .OneWayBind(ViewModel, vm => vm.CommitReorder, v => v.CommitReorder.Command)
                    .DisposeWith(disposables);

                this
                    .OneWayBind(ViewModel, vm => vm.ItemTapped, v => v.ItemTapped.Command)
                    .DisposeWith(disposables);
            });
        }
    }
}