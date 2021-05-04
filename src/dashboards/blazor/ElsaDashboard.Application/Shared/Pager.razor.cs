using System;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Application.Shared
{
    partial class Pager
    {
        [Parameter] public int? Page { get; set; }
        [Parameter] public int? PageSize { get; set; }
        [Parameter] public int TotalCount { get; set; }

        private int From => Page!.Value * PageSize!.Value + 1;
        private int To => Math.Min(From + PageSize!.Value, TotalCount);
        private int PageCount => (int) (((float) TotalCount - 1) / PageSize!.Value) + 1;
    }
}