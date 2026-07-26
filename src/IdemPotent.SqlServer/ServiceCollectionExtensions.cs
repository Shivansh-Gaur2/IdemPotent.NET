using IdemPotent.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IdemPotent.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseSqlServerStore(
        this IServiceCollection services,
        string connectionString,
        Action<SqlServerStoreOptions>? configure = null)
    {
        var options = new SqlServerStoreOptions();
        configure?.Invoke(options);

        if (options.AutoCreateSchema)
        {
            EnsureSchemaCreated(connectionString);
        }

        services.AddScoped<IIdempotencyStore>(_ => new SqlIdempotencyStore(connectionString));

        return services;
    }

    private static void EnsureSchemaCreated(string connectionString)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "IdemPotent.SqlServer.Scripts.CreateIdempotencyTable.sql";

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