using Keyloop.ServiceScheduler.Core.Entities;

namespace Keyloop.ServiceScheduler.Api.Contracts
{
    public sealed record AppointmentResponse(
    Guid Id,
    Guid DealershipId,
    Guid VehicleId,
    Guid CustomerId,
    Guid ServiceTypeId,
    Guid TechnicianId,
    Guid ServiceBayId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    AppointmentStatus Status);
}
