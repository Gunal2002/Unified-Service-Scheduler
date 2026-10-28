using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyloop.ServiceScheduler.Core.Entities
{
    public sealed class ServiceType
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DealershipId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string RequiredSkill { get; set; } = string.Empty;
    }

}
