using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class Thumbprint
    {
        public string crv { get; set; }
        public string kty { get; set; }
        public string x { get; set; }
        public string y { get; set; }
    }

}
