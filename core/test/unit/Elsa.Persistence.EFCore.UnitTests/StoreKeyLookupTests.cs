using System.Linq.Expressions;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elsa.Persistence.EFCore.UnitTests;

public class StoreKeyLookupTests
{
    [Fact]
    public async Task DifferentIdsReuseTheCompiledLookup()
    {
        var compilations = 0;
        var options = new DbContextOptionsBuilder<OwnershipDbContext>()
            .UseSqlite("Data Source=:memory:")
            .EnableServiceProviderCaching(false)
            .LogTo((id, _) => id == CoreEventId.QueryCompilationStarting, _ => compilations++)
            .Options;
        await using var context = new OwnershipDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var existing = new OwnedRow { Id = "existing" };
        context.Add(existing);
        await context.SaveChangesAsync();

        Expression<Func<OwnedRow, string>> selector = row => row.Id;
        Assert.True(await context.Rows.AnyAsync(selector.BuildEqualsExpression(existing)));
        Assert.Equal(1, compilations);
        Assert.False(await context.Rows.AnyAsync(selector.BuildEqualsExpression(new OwnedRow { Id = "missing" })));
        Assert.Equal(1, compilations);
    }
}
