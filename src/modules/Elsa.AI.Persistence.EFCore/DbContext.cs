using Elsa.AI.Persistence.EFCore.Entities;

namespace Elsa.AI.Persistence.EFCore;

public class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<AiProposalRecord> Proposals => Set<AiProposalRecord>();
    public DbSet<AiAuditRecord> AuditRecords => Set<AiAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiProposalRecord>(entity =>
        {
            entity.ToTable("AiProposals");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ConversationId });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.WorkflowPayload).IsRequired();
            entity.Property(x => x.ValidationDiagnostics).IsRequired();
            entity.Property(x => x.Warnings).IsRequired();
        });

        modelBuilder.Entity<AiAuditRecord>(entity =>
        {
            entity.ToTable("AiAuditRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ConversationId });
            entity.HasIndex(x => x.ActorId);
            entity.HasIndex(x => new { x.TenantId, x.Timestamp });
            entity.HasIndex(x => x.ProposalId);
            entity.HasIndex(x => x.ToolInvocationId);
            entity.Property(x => x.Type).IsRequired();
            entity.Property(x => x.Data).IsRequired();
        });
    }
}
