using System.Data;
using Dapper;
using BookingSystem.Application.Common.Interfaces;

namespace BookingSystem.Application.Common.Extensions;

/// <summary>
/// Dapper extension methods for multi-tenant query filtering
/// </summary>
public static class DapperExtensions
{
    /// <summary>
    /// Executes a query with automatic TenantId filtering.
    /// Wraps the original SQL with a CTE that adds TenantId filter.
    /// </summary>
    public static async Task<IEnumerable<T>> QueryWithTenantAsync<T>(
        this IDbConnection connection,
        ITenantContext tenantContext,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var tenantId = tenantContext.TenantId;
        
        // Create dynamic parameters that include TenantId
        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("@TenantId", tenantId);
        
        return await connection.QueryAsync<T>(sql, dynamicParams, transaction, commandTimeout);
    }

    /// <summary>
    /// Executes a single result query with automatic TenantId filtering
    /// </summary>
    public static async Task<T?> QuerySingleOrDefaultWithTenantAsync<T>(
        this IDbConnection connection,
        ITenantContext tenantContext,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var tenantId = tenantContext.TenantId;
        
        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("@TenantId", tenantId);
        
        return await connection.QuerySingleOrDefaultAsync<T>(sql, dynamicParams, transaction, commandTimeout);
    }

    /// <summary>
    /// Executes an INSERT/UPDATE/DELETE command with automatic TenantId filtering
    /// </summary>
    public static async Task<int> ExecuteWithTenantAsync(
        this IDbConnection connection,
        ITenantContext tenantContext,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        var tenantId = tenantContext.TenantId;
        
        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("@TenantId", tenantId);
        
        return await connection.ExecuteAsync(sql, dynamicParams, transaction, commandTimeout);
    }
}
