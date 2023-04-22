using Elsa.Samples.Onboarding.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Samples.Onboarding.Web.Data;

public class OnboardingDbContext : DbContext
{
    public OnboardingDbContext(DbContextOptions options) : base(options)
    {
        
    }
    
    public DbSet<OnboardingTask> Tasks { get; set; } = default!;
}