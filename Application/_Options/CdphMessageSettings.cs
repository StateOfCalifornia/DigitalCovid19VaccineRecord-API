using Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class CdphMessageSettings : ISettingsValidate
    {
        [Display(Name = "CDPHMessageSettings.SMSApi")]
        [Required(AllowEmptyStrings = false)]
        public string SmsApi { get; set; }

        [Display(Name = "CDPHMessageSettings.SMSKey")]
        [Required(AllowEmptyStrings = false)]
        public string SmsKey { get; set; }

        [Display(Name = "CDPHMessageSettings.SandBox")]
        [Required(AllowEmptyStrings = false)]
        public string SandBox { get; set; }

        #region IOptionsValidatable Implementation
        public void Validate() => Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        #endregion
    }
}
