using FluentValidation;
using System;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class GetVaccineCredentialQueryValidator : AbstractValidator<GetVaccineCredentialQuery>
    {
        public GetVaccineCredentialQueryValidator()
        {
            RuleFor(c => c.Id)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(300);

            RuleFor(c => c.Pin)
                .NotEmpty()
                .MinimumLength(4)
                .MaximumLength(4);

            RuleFor(c => c.WalletCode)
                .Length(1)
                .When(c => c.WalletCode != null && c.WalletCode != "");
        }
    }
}
