using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VaccineCredential.Api.Options
{
    public class SwaggerSettings
    {
        public string ProjectName { get; set; }
        public string DocumentName { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OpenApiReferenceID { get; set; }
        public Contact Contact { get; set; }
    }

    public class Contact
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
    }
}
