using FluentValidation;

namespace BookingSystem.Application.Features.Resources.Queries.GetAllResources;

/// <summary>
/// Validator for GetAllResourcesQuery
/// </summary>
public class GetAllResourcesQueryValidator : AbstractValidator<GetAllResourcesQuery>
{
    public GetAllResourcesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

        RuleFor(x => x.ResourceType)
            .MaximumLength(100).WithMessage("Resource type must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.ResourceType));
    }
}
