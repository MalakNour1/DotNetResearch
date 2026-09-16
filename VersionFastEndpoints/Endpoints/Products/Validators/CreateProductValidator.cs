using FastEndpoints;
using FluentValidation;
using ProductShared.DTOs;

namespace FastEndpointsVersion.Endpoints.Products.Validators;

public class CreateProductValidator : Validator<ProductRequestDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0.");
    }
}