using Elsa.AI.Persistence.EFCore.Entities;
using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore;

public class AIDbContext : ElsaDbContextBase
{
    private static readonly IServiceProvider EmptyServiceProvider = new ServiceCollection().BuildServiceProvider();

    public AIDbContext(DbContextOptions<AIDbContext> options) : this(options, EmptyServiceProvider)
    {
    }

    public AIDbContext(DbContextOptions<AIDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    public DbSet<AIConversationRecord> Conversations => Set<AIConversationRecord>();
    public DbSet<AIProposalRecord> Proposals => Set<AIProposalRecord>();
    public DbSet<AIAuditRecord> AuditRecords => Set<AIAuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AIProposalRecord>(entity =>
        {
            entity.ToTable("AIProposals");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ConversationId });
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.WorkflowPayload).IsRequired();
            entity.Property(x => x.ValidationDiagnostics).IsRequired();
            entity.Property(x => x.Warnings).IsRequired();
        });

        modelBuilder.Entity<AIAuditRecord>(entity =>
        {
            entity.ToTable("AIAuditRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ConversationId });
            entity.HasIndex(x => x.ActorId);
            entity.HasIndex(x => new { x.TenantId, x.Timestamp });
            entity.HasIndex(x => x.ProposalId);
            entity.HasIndex(x => x.ToolInvocationId);
            entity.Property(x => x.Type).IsRequired();
            entity.Property(x => x.Data).IsRequired();
        });

        modelBuilder.Entity<AIConversationRecord>(entity =>
        {
            entity.ToTable("AIConversations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.UserId });
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.RetentionExpiresAt);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.RetentionMode).IsRequired();
            entity.Property(x => x.Messages).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
