using FluentValidation;

namespace BookingSystem.Application.Features.Resources.Queries.GetResourceById;

/// <summary>
/// Validator for GetResourceByIdQuery
/// </summary>
public class GetResourceByIdQueryValidator : AbstractValidator<GetResourceByIdQuery>
{
    public GetResourceByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Resource ID is required");
    }
}
