using System.Collections.Generic;

namespace Elsa.Design
{
    public class SelectList
    {
        public bool IsFlagsEnum { get; set; }
        public ICollection<SelectListItem> Items { get; set; } = new List<SelectListItem>();
    }
}