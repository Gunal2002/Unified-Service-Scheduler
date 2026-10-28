using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyloop.ServiceScheduler.Core.Entities
{
    public sealed class Dealership
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string TimeZoneId { get; set; } = "UTC";
    }
}
