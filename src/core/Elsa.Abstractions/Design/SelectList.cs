using System.Collections.Generic;

namespace Elsa.Design
{
    public class SelectList
    {
        public SelectList()
        {
        }

        public SelectList(ICollection<SelectListItem> items, bool isFlagsEnum = false)
        {
            Items = items;
            IsFlagsEnum = isFlagsEnum;
        }
        
        public bool IsFlagsEnum { get; set; }
        public ICollection<SelectListItem> Items { get; set; } = new List<SelectListItem>();
    }
}