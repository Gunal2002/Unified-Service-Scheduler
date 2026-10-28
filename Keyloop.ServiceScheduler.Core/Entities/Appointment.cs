using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyloop.ServiceScheduler.Core.Entities
{
    public enum AppointmentStatus
    {
        Confirmed = 1,
        Cancelled = 2
    }

    public sealed class Appointment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DealershipId { get; set; }

        public Guid VehicleId { get; set; }

        public Guid CustomerId { get; set; }

        public Guid ServiceTypeId { get; set; }

        public Guid TechnicianId { get; set; }

        public Guid ServiceBayId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public AppointmentStatus Status { get; set; }
            = AppointmentStatus.Confirmed;

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public bool HasValidTimeRange()
        {
            return EndTime > StartTime;
        }

        public bool OverlapsWith(Appointment other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (Status != AppointmentStatus.Confirmed ||
                other.Status != AppointmentStatus.Confirmed)
            {
                return false;
            }

            return StartTime < other.EndTime &&
                   EndTime > other.StartTime;
        }
    }
}
