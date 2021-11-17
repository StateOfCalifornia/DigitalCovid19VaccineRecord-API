using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class CdphSmsMessagingClient
    {
        public HttpClient SmsClient { get; private set; }

        public CdphSmsMessagingClient(HttpClient client)
        {
            SmsClient = client;
        }
    }
}
