using Application.Common.Interfaces;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class PinpointEmailSettings : ISettingsValidate
    {
        [Display(Name = "PinpointEmailSettings.EmailApi")]
        [Required(AllowEmptyStrings = false)]
        public string EmailApi { get; set; }

        [Display(Name = "PinpointEmailSettings.EmailKey")]
        [Required(AllowEmptyStrings = false)]
        public string EmailKey { get; set; }

        [Display(Name = "PinpointEmailSettings.SandBox")]
        [Required(AllowEmptyStrings = false)]
        public string SandBox { get; set; }

        #region IOptionsValidatable Implementation
        public void Validate() => Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        #endregion
    }
}
