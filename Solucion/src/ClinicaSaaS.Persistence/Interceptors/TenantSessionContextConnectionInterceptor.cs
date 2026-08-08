using System.Data.Common;
using ClinicaSaaS.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClinicaSaaS.Persistence.Interceptors;

/// <summary>
/// Preparación explícita para Row-Level Security de SQL Server, recomendada como segunda
/// capa de aislamiento multi-tenant en el documento de diseño de BD (sección 10.2): "una
/// política de seguridad de SQL Server (CREATE SECURITY POLICY) ligada a
/// SESSION_CONTEXT('ClinicaId'), seteado por EF Core al abrir cada conexión".
///
/// Este interceptor SOLO hace la mitad de "aplicación": setea SESSION_CONTEXT en cada
/// conexión nueva. La política RLS en sí (CREATE SECURITY POLICY + función de filtro) es
/// trabajo de BD explícitamente fuera de alcance de este documento y de esta fase — el propio
/// diseño de BD lo deja como "próximo paso... con el DBA antes de ir a producción" (sección 12).
/// Sin la política creada en SQL Server, este SESSION_CONTEXT no tiene ningún efecto todavía;
/// se deja listo para que activarla más adelante sea solo un cambio de BD, sin tocar Persistence.
/// </summary>
public sealed class TenantSessionContextConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection && sqlConnection.State != System.Data.ConnectionState.Open)
        {
            await sqlConnection.OpenAsync(cancellationToken);
            await EstablecerSessionContextAsync(sqlConnection, cancellationToken);
            return InterceptionResult.Suppress();
        }

        return result;
    }

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        if (connection is SqlConnection sqlConnection && sqlConnection.State != System.Data.ConnectionState.Open)
        {
            sqlConnection.Open();
            EstablecerSessionContextAsync(sqlConnection, CancellationToken.None).GetAwaiter().GetResult();
            return InterceptionResult.Suppress();
        }

        return result;
    }

    private async Task EstablecerSessionContextAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sp_set_session_context @key = N'ClinicaId', @value = @clinicaId;";

        var parametro = command.CreateParameter();
        parametro.ParameterName = "@clinicaId";
        parametro.Value = (object?)tenantContext.ClinicaId ?? DBNull.Value;
        command.Parameters.Add(parametro);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
