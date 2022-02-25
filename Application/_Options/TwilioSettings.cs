using Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class TwilioSettings : ISettingsValidate
    {
        [Display(Name = "TwilioSettings.AccountSID")]
        [Required(AllowEmptyStrings = true)]
        public string AccountSID { get; set; }

        [Display(Name = "TwilioSettings.AuthToken")]
        [Required(AllowEmptyStrings = true)]
        public string AuthToken { get; set; }
        [Display(Name = "TwilioSettings.FromPhone")]
        [Required(AllowEmptyStrings = true)]
        public string FromPhone { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string SandBox { get; set; }

        #region IOptionsValidatable Implementation
        public void Validate()
        {
            Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        }
        #endregion
    }
}
