using Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.VaccineCredential.Queries.GetVaccineStatus
{
    public class StatusModel
    {
        public bool ViewStatus { get; set; }
        public bool InvalidPin { get; set; }
        public RateLimit RateLimit { get; set; }
    }
}
