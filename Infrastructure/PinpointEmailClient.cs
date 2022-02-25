using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class PinpointEmailClient
    {
        public HttpClient EmailClient { get; private set; }

        public PinpointEmailClient(HttpClient client)
        {
            EmailClient = client;
        }
    }
}
