using IdemShield.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace IdemShield.SqlServer.Tests;

public class SqlServerStoreOptionsTests
{
    [Fact]
    public void Defaults_enable_hourly_cleanup_in_batches_of_one_thousand()
    {
        var options = new SqlServerStoreOptions();

        Assert.True(options.EnableCleanup);
        Assert.Equal(TimeSpan.FromHours(1), options.CleanupInterval);
        Assert.Equal(1000, options.CleanupBatchSize);
    }

    [Fact]
    public void Registration_adds_cleanup_service_by_default()
    {
        var services = new ServiceCollection();

        services.UseSqlServerStore("Server=not-used;", options => options.AutoCreateSchema = false);
        using var provider = services.BuildServiceProvider();

        Assert.Single(provider.GetServices<IHostedService>());
    }

    [Fact]
    public void Registration_omits_cleanup_service_when_disabled()
    {
        var services = new ServiceCollection();

        services.UseSqlServerStore("Server=not-used;", options =>
        {
            options.AutoCreateSchema = false;
            options.EnableCleanup = false;
        });
        using var provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IHostedService>());
    }

    [Fact]
    public void Registration_rejects_non_positive_cleanup_values()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.UseSqlServerStore("Server=not-used;", options =>
            {
                options.AutoCreateSchema = false;
                options.CleanupBatchSize = 0;
            }));
    }
}
