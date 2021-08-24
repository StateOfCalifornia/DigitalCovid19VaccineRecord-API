using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace VaccineCredential.Api.Common.Filters
{
    public class OptionsValidationStartupFilter : IStartupFilter
    {
        private readonly IEnumerable<IOptionsValidatable> _validatableObjects;

        #region Constructor
        public OptionsValidationStartupFilter(IEnumerable<IOptionsValidatable> validationObjects)
        {
            _validatableObjects = validationObjects;
        }
        #endregion

        #region IStartupFilter Implementation
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            foreach (var validatableObject in _validatableObjects)
            {
                validatableObject.Validate();
            }

            //don't alter the configuration
            return next;
        }
        #endregion
    }
}
