using Elsa.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class OracleContext:ElsaContext
    {
        public OracleContext(DbContextOptions options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ActivityDefinitionEntity>(entity =>
            {
                entity.Property(x => x.Description)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.DisplayName)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Name)
                    .HasColumnType("NCLOB");
            });

            modelBuilder.Entity<ActivityInstanceEntity>(entity =>
            {
                entity.Property(x => x.Output)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.State)
                    .HasColumnType("NCLOB");
            });

            modelBuilder.Entity<ConnectionDefinitionEntity>(entity =>
            {
                entity.Property(x => x.Outcome)
                    .HasColumnType("NCLOB");
            });

            modelBuilder.Entity<WorkflowDefinitionVersionEntity>(entity =>
            {
                entity.Property(x => x.Description)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Name)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Variables)
                    .HasColumnType("NCLOB");
            });

            modelBuilder.Entity<WorkflowInstanceEntity>(entity =>
            {
                entity.Property(x => x.ExecutionLog)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Fault)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Input)
                    .HasColumnType("NCLOB");

                entity.Property(x => x.Scope)
                    .HasColumnType("NCLOB");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
