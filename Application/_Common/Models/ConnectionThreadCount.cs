using Snowflake.Data.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Models
{
    public class ConnectionThreadCount
    {
        public SnowflakeDbConnection Connection { get; set; }
        public int TaskCount;
    }

}
