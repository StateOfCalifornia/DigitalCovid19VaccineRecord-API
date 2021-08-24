using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class RateLimit
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }
}
