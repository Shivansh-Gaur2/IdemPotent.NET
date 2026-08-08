using IdemShield.AspNetCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace IdemShield.SqlServer;

/// <summary>Registers the SQL Server provider for IdemShield.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Uses SQL Server as the application's idempotency store.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configure">An optional provider configuration callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection UseSqlServerStore(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerStoreOptions>? configure = null)
    {
        var options = new SqlServerStoreOptions();
        configure?.Invoke(options);
        options.Validate();

        if (options.AutoCreateSchema)
        {
            EnsureSchemaCreated(connectionString);
        }

        services.AddScoped<IIdempotencyStore>(_ => new SqlIdempotencyStore(connectionString));

        if (options.EnableCleanup)
        {
            services.AddLogging();
            services.AddSingleton(options);
            services.AddSingleton(new SqlExpiredRecordCleanup(connectionString, options.CleanupBatchSize));
            services.AddHostedService<SqlExpiredRecordCleanupService>();
        }

        return services;
    }

    private static void EnsureSchemaCreated(string connectionString)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "IdemShield.SqlServer.Scripts.CreateIdempotencyTable.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd();

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(script, connection);
        command.ExecuteNonQuery();
    }
}
