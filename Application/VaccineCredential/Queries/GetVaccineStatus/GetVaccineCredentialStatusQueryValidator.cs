using FluentValidation;
using System;

namespace Application.VaccineCredential.Queries.GetVaccineStatus
{
    public class GetVaccineCredentialStatusQueryValidator : AbstractValidator<GetVaccineCredentialStatusQuery>
    {
        public GetVaccineCredentialStatusQueryValidator()
        {
            var minDate = Convert.ToDateTime("1900-01-01");
            var maxDate = DateTime.Now;

            RuleFor(c => c.FirstName)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(50);
            RuleFor(c => c.LastName)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(50);
            RuleFor(c => c.PhoneNumber)
               .MaximumLength(12)
               .MinimumLength(12)
               .When(c => c.PhoneNumber != null && c.PhoneNumber != "");
            RuleFor(c => c.PhoneNumber)
               .NotEmpty()
               .NotNull()
               .MaximumLength(12)
               .MinimumLength(12)
               .When(c => c.EmailAddress == null || c.EmailAddress == "");
            RuleFor(c => c.EmailAddress)
                .NotEmpty()
                .NotNull()
               .MaximumLength(50)
               .MinimumLength(3)
               .EmailAddress()
               .When(c => c.EmailAddress != null && c.EmailAddress != "");
            RuleFor(c => c.EmailAddress)
                .NotEmpty()
                .NotNull()
               .MaximumLength(50)
               .MinimumLength(3)
               .EmailAddress()
               .When(c => c.PhoneNumber == null || c.PhoneNumber == "");
            RuleFor(c => c.DateOfBirth)
                .GreaterThan(minDate)
                .LessThan(maxDate);
            RuleFor(c => c.EmailAddress)
               .Empty()
               .When(c => c.PhoneNumber != "" && c.PhoneNumber != null);
            RuleFor(c => c.PhoneNumber)
               .Empty()
               .When(c => c.EmailAddress != "" && c.EmailAddress != null);
            RuleFor(c => c.Pin)
                .NotEmpty()
                .MinimumLength(4)
                .MaximumLength(4);
            RuleFor(c => c.Language)
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(2);

        }
    }
}
