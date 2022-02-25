using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class EmailRequest
    {
        public string RecipientEmail { get; set;}
        public string RecipientName { get; set;}
        public string Subject { get; set;}
        public string HtmlContent { get; set;}
        public string PlainTextContent { get; set;}
    }
}
