using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Runtime.Serialization;

namespace TkXamListViewIssue.Models
{
    public class Item : ReactiveObject
    {
        public Item(int id, string name, bool isSelected, int? displayOrder)
        {
            Id = id;
            Name = name;
            IsSelected = isSelected;
            DisplayOrder = displayOrder;
        }

        [Reactive]
        [DataMember]
        public int Id { get; set; }

        [Reactive]
        [DataMember]
        public string Name { get; set; }

        [Reactive]
        [DataMember]
        public bool IsSelected { get; set; }

        [Reactive]
        [DataMember]
        public int? DisplayOrder { get; set; }
    }
}