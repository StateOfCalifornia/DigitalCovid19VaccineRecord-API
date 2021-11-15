using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class Thumbprint
    {
        [JsonProperty("crv")]
        public string Crv { get; set; }
        [JsonProperty("kty")]
        public string Kty { get; set; }
        [JsonProperty("x")]
        public string X { get; set; }
        [JsonProperty("y")]
        public string Y { get; set; }
    }

}
