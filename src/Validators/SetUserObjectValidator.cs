using FluentValidation;
using MO.MODBApi.DTOs;

namespace MO.MODBApi.Validators{
    public class SetUserObjectValidator : AbstractValidator<SetUserObject>{
      public SetUserObjectValidator()
      {
          RuleFor(obj => obj.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required.");
          
          RuleFor(obj => obj.Collection)
            .NotNull()
            .NotEmpty()
            .Matches("^[a-zA-Z0-9@._-]+$")
            .WithMessage("Collection must match ^[a-zA-Z0-9@._-]+$.");
      }
  }
}