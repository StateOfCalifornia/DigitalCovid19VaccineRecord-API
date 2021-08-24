using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class GetVaccineCredentialQuery : IRequest<VaccineCredentialModel>
    {
        /// <summary>
        /// REQUIRED. Unique Id
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>        
        /// REQUIRED. Secret Pin
        /// </summary>
        [Required]
        public string Pin { get; set; }

        /// <summary>
        /// Optional. WalletCode
        /// For exmple: 'A' for Apple, 'G' for Google
        /// </summary>
        public string WalletCode { get; set; }


    }
}
