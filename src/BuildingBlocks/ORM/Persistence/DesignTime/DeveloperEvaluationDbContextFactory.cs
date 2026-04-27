using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.DesignTime;

public sealed class DeveloperEvaluationDbContextFactory : IDesignTimeDbContextFactory<DeveloperEvaluationDbContext>
{
    public DeveloperEvaluationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<DeveloperEvaluationDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("DEVELOPER_EVALUATION_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=developer_evaluation;Username=postgres;Password=postgres";

        builder.UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(DeveloperEvaluationDbContext).Assembly.FullName));
        return new DeveloperEvaluationDbContext(builder.Options);
    }
}