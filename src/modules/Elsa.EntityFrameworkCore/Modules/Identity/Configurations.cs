using System.Linq.Expressions;
using Elsa.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Modules.Identity;

internal class Configurations : IEntityTypeConfiguration<User>, IEntityTypeConfiguration<Application>, IEntityTypeConfiguration<Role>
{
    private static Expression<Func<ICollection<string>, string>> StringCollectionToStringConverter => v => string.Join(",", v);
    private static Expression<Func<string, ICollection<string>>> StringToStringCollectionConverter => v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

    private static readonly ValueComparer<ICollection<string>> StringCollectionComparer = new(
        (c1, c2) => c1!.SequenceEqual(c2!),
        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
        c => c.ToList());

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(User)}_{nameof(User.Name)}").IsUnique();
        builder.Property(x => x.Roles).HasColumnName("Roles").HasConversion(StringCollectionToStringConverter, StringToStringCollectionConverter).IsRequired().Metadata.SetValueComparer(StringCollectionComparer);
    }

    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasIndex(x => x.ClientId).HasDatabaseName($"IX_{nameof(Application)}_{nameof(Application.ClientId)}").IsUnique();
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(Application)}_{nameof(Application.Name)}").IsUnique();
        builder.Property(x => x.Roles).HasColumnName("Roles").HasConversion(StringCollectionToStringConverter, StringToStringCollectionConverter).IsRequired().Metadata.SetValueComparer(StringCollectionComparer);
    }

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(Role)}_{nameof(Role.Name)}").IsUnique();
        builder.Property(x => x.Permissions).HasColumnName("Permissions").HasConversion(StringCollectionToStringConverter, StringToStringCollectionConverter).IsRequired().Metadata.SetValueComparer(StringCollectionComparer);
    }
}