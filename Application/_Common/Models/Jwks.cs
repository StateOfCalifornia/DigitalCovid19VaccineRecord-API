using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class Key
    {
        public string Kty { get; set; }
        public string Kid { get; set; }
        public string Use { get; set; }
        public string Alg { get; set; }
        public string Crv { get; set; }
        public string X { get; set; }
        public string Y { get; set; }

    }
    public class Jwks
    {
        public List<Key> Keys { get; set; }
    }
}
