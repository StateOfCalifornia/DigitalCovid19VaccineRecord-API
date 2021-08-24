using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;

namespace Application.Options
{
    public class SnowFlakeSettings : ISettingsValidate
    {
        [Display(Name = "SnowFlakeSettings.ConnectionString")]
        [Required(AllowEmptyStrings = false)]
        public string ConnectionString { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string StatusPhoneQuery { get; set; }
        public string StatusEmailQuery { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string IdQuery { get; set; }

        public string RelaxedPhoneQuery { get; set; }
        public string RelaxedEmailQuery { get; set; }

        public string UseRelaxed { get; set; }


        #region IOptionsValidatable Implementation
        public void Validate()
        {
            Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        }
        #endregion
    }
}
