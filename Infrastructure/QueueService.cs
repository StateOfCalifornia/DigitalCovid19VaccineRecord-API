using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Options;
using Azure.Storage.Queues;


namespace Infrastructure
{
    public class QueueService : IQueueService
    {

        private readonly MessageQueueSettings _mqSettings;
        private readonly IEnumerable<QueueClient> _queueClients;

        public QueueService(IEnumerable<QueueClient> qClients, MessageQueueSettings mqSettings)
        {
            _mqSettings = mqSettings;
            _queueClients = qClients;
        }

        public async Task<bool> AddMessageAsync(string message)
        {
            // Get the connection string from app settings [Queue1]
            var client = GetClientByName(_mqSettings.QueueName);
            var res = await client.SendMessageAsync(message, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(-1));

            if (!string.IsNullOrWhiteSpace(_mqSettings.AlternateQueueName))
            {
                try{
                    // Get the connection string from app settings [Queue2]
                    var clientAlternate = GetClientByName(_mqSettings.AlternateQueueName);
                    var resAlternate = await clientAlternate.SendMessageAsync(message, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(-1));
                } catch {}                
            }            

            return !string.IsNullOrEmpty(res.Value.MessageId);
        }

        private QueueClient GetClientByName(string name)
        {
            foreach(var c in _queueClients)
            {
                if (c.Name.Equals(name))
                {
                    return c;
                }
            }
            return null;
        }
    }
}
