namespace Keyloop.ServiceScheduler.Api.Contracts
{
    public sealed class CreateAppointmentRequest
    {
        public Guid DealershipId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid ServiceTypeId { get; set; }
        public DateTimeOffset StartTime { get; set; }
    }
}
