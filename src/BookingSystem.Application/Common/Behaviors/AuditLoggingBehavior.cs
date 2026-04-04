using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace BookingSystem.Application.Common.Behaviors;

public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;

    public AuditLoggingBehavior(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute the request first
        var response = await next();

        // Only audit commands (not queries)
        var requestName = typeof(TRequest).Name;
        if (!IsAuditableCommand(requestName))
        {
            return response;
        }

        // Log audit entry asynchronously (fire-and-forget to not block response)
        _ = Task.Run(async () =>
        {
            try
            {
                var (entityName, action) = ParseCommandName(requestName);
                var entityId = ExtractEntityId(request, response);

                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = action,
                    OldValues = action == "Create" ? null : SerializeOldValues(request),
                    NewValues = action == "Delete" ? null : SerializeNewValues(response),
                    UserId = _currentUserService.UserId,
                    IpAddress = null, // Could extract from HttpContext if needed
                    Reason = null
                };

                await _auditLogRepository.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - audit logging should not break the request
                Console.WriteLine($"Audit logging failed: {ex.Message}");
            }
        }, cancellationToken);

        return response;
    }

    private static bool IsAuditableCommand(string commandName)
    {
        // Only audit commands (CUD operations), not queries
        return commandName.Contains("Command", StringComparison.OrdinalIgnoreCase) &&
               (commandName.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
                commandName.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                commandName.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                commandName.Contains("Cancel", StringComparison.OrdinalIgnoreCase) ||
                commandName.Contains("Confirm", StringComparison.OrdinalIgnoreCase));
    }

    private static (string EntityName, string Action) ParseCommandName(string commandName)
    {
        // Example: "CreateBookingCommand" → ("Booking", "Create")
        // Example: "UpdateResourceCommand" → ("Resource", "Update")
        // Example: "CancelBookingCommand" → ("Booking", "Cancel")

        var action = commandName switch
        {
            var name when name.Contains("Create", StringComparison.OrdinalIgnoreCase) => "Create",
            var name when name.Contains("Update", StringComparison.OrdinalIgnoreCase) => "Update",
            var name when name.Contains("Delete", StringComparison.OrdinalIgnoreCase) => "Delete",
            var name when name.Contains("Cancel", StringComparison.OrdinalIgnoreCase) => "Cancel",
            var name when name.Contains("Confirm", StringComparison.OrdinalIgnoreCase) => "Confirm",
            _ => "Unknown"
        };

        // Extract entity name: remove action and "Command" suffix
        var entityName = commandName
            .Replace("Create", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Update", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Delete", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Cancel", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Confirm", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Command", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return (entityName, action);
    }

    private static Guid ExtractEntityId(TRequest request, TResponse response)
    {
        // Try to get Id from response first (for Create operations)
        var responseType = response?.GetType();
        var idProperty = responseType?.GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            var id = idProperty.GetValue(response);
            if (id is Guid guid && guid != Guid.Empty)
            {
                return guid;
            }
        }

        // Try to get Id from request (for Update/Delete operations)
        var requestType = request.GetType();
        idProperty = requestType.GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            var id = idProperty.GetValue(request);
            if (id is Guid guid && guid != Guid.Empty)
            {
                return guid;
            }
        }

        // Try BookingId property (for Cancel/Confirm operations)
        var bookingIdProperty = requestType.GetProperty("BookingId");
        if (bookingIdProperty != null && bookingIdProperty.PropertyType == typeof(Guid))
        {
            var id = bookingIdProperty.GetValue(request);
            if (id is Guid guid && guid != Guid.Empty)
            {
                return guid;
            }
        }

        return Guid.Empty;
    }

    private static string? SerializeOldValues(TRequest request)
    {
        try
        {
            // For Update operations, serialize the request (contains old state in UpdateDto)
            // This is a simplified version - in production, you'd fetch actual old state from DB
            return JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeNewValues(TResponse response)
    {
        try
        {
            // Serialize the response which typically contains the new state
            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return null;
        }
    }
}
