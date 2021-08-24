using Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Options
{
    public class SendGridSettings : ISettingsValidate
    {
        [Display(Name = "SendGridSettings.Key")]
        [Required(AllowEmptyStrings = false)]
        public string Key { get; set; }

        [Display(Name = "SendGridSettings.Sender")]
        [Required(AllowEmptyStrings = false)]
        public string Sender { get; set; }

        [Display(Name = "SendGridSettings.SenderEmail")]
        [Required(AllowEmptyStrings = false)]
        public string SenderEmail { get; set; }

        [Display(Name = "SendGridSettings.SandBox")]
        [Required(AllowEmptyStrings = false)]
        public string SandBox { get; set; }

        public void Validate()
        {
            //DataAnnotation Validation
            Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
        }
    }
}
