using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;

namespace Application.Options
{
    public class KeySettings : ISettingsValidate
    {
        [Display(Name = "KeySettings.PrivateKey")]
        [Required(AllowEmptyStrings = false)]
        public string PrivateKey { get; set; }


        [Display(Name = "KeySettings.Certificate")]
        [Required(AllowEmptyStrings = false)]
        public string Certificate { get; set; }

        [Display(Name = "KeySettings.Issuer")]
        [Required(AllowEmptyStrings = false)]
        public string Issuer { get; set; }

        [Display(Name = "KeySettings.GooglePrivateKey")]
        [Required(AllowEmptyStrings = false)]
        public string GooglePrivateKey { get; set; }


        [Display(Name = "KeySettings.GoogleCertificate")]
        [Required(AllowEmptyStrings = false)]
        public string GoogleCertificate { get; set; }

        [Display(Name = "KeySettings.GoogleIssuer")]
        [Required(AllowEmptyStrings = false)]
        public string GoogleIssuer { get; set; }

        [Display(Name = "KeySettings.GoogleIssuerId")]
        [Required(AllowEmptyStrings = false)]
        public string GoogleIssuerId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string GoogleWalletLogo { get; set; }

        #region IOptionsValidatable Implementation
        public void Validate()
        {
            Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        }
        #endregion
    }
}
