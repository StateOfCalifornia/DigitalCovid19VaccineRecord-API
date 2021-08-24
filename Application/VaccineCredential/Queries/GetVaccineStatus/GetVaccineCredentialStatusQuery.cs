using System;
using System.ComponentModel.DataAnnotations;
using MediatR;


namespace Application.VaccineCredential.Queries.GetVaccineStatus
{
    public class GetVaccineCredentialStatusQuery : IRequest<StatusModel>
    {
        /// <summary>
        /// REQUIRED. User's First Name.
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// REQUIRED. User's Last Name.
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// REQUIRED. User's Date of Birth.
        /// </summary>
        [Required]
        public DateTime? DateOfBirth { get; set; }
             
        /// <summary>
        /// User's Phone Number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// User's Email address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>        
        /// REQUIRED. Secret Pin
        /// </summary>
        [Required]
        public string Pin { get; set; }

        /// <summary>        
        /// REQUIRED. Language
        /// </summary>
        [Required]
        public string Language { get; set; }

    }
}
