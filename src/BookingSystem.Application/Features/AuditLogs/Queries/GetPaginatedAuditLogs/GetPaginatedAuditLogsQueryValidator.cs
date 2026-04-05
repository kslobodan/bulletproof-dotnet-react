using FluentValidation;

namespace BookingSystem.Application.Features.AuditLogs.Queries.GetPaginatedAuditLogs;

public class GetPaginatedAuditLogsQueryValidator : AbstractValidator<GetPaginatedAuditLogsQuery>
{
    public GetPaginatedAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be after or equal to FromDate");

        RuleFor(x => x.Action)
            .Must(action => string.IsNullOrEmpty(action) || new[] { "Create", "Update", "Delete", "Cancel", "Confirm" }.Contains(action))
            .When(x => !string.IsNullOrEmpty(x.Action))
            .WithMessage("Action must be one of: Create, Update, Delete, Cancel, Confirm");
    }
}
