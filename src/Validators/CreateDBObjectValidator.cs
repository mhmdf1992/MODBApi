using FluentValidation;
using MO.MODBApi.DTOs;

namespace MO.MODBApi.Validators{
    public class CreateDBObjectValidator : AbstractValidator<CreateDBObject>{
      public CreateDBObjectValidator()
      {
          RuleFor(obj => obj.Name)
            .NotNull()
            .NotEmpty()
            .Matches("^[a-zA-Z0-9@._-]+$")
            .WithMessage("Database name must match ^[a-zA-Z0-9@._-]+$.");
      }
  }
}