using System.Linq;
using FluentValidation;
using MO.MODB;
using MO.MODBApi.DTOs;

namespace MO.MODBApi.Validators{
    public class SetObjectValidator : AbstractValidator<SetObject>{
      public SetObjectValidator()
      {
          RuleFor(obj => obj.Key)
            .NotNull()
            .NotEmpty()
            .WithMessage("Key is required.");
          
          RuleFor(obj => obj.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage("Type is required.")
            .Must(type => Converter.To.Keys.Contains(type))
            .WithMessage($"Type not supported.\nSupportedTypes: {string.Join(',', Converter.To.Keys)}.");

          RuleForEach(obj => obj.Indices)
            .SetValidator(new SetObjectIndexItemValidator());
      }
  }

  public class SetObjectIndexItemValidator : AbstractValidator<SetObjectIndexItem>{
    public SetObjectIndexItemValidator(){
      RuleFor(obj => obj.Name)
        .NotNull()
        .NotEmpty()
        .WithMessage("Name is required.")
        .Matches("^[a-zA-Z0-9@_-]+$")
        .WithMessage("Index names must match ^[a-zA-Z0-9@_-]+$");
      
      RuleFor(obj => obj.Type)
        .NotNull()
        .NotEmpty()
        .WithMessage("Type is required.")
        .Must(type => Converter.To.Keys.Contains(type))
        .WithMessage($"Type not supported.\nSupportedTypes: {string.Join(',', Converter.To.Keys)}.");
    }
  }
}