using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Connections.Persistence.EntityFrameworkCore;
public class Configurations : IEntityTypeConfiguration<ConnectionDefinition>
{
    public void Configure(EntityTypeBuilder<ConnectionDefinition> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(ConnectionDefinition)}_{nameof(ConnectionDefinition.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(ConnectionDefinition)}_{nameof(ConnectionDefinition.TenantId)}");
    }
}
