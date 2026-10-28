using Keyloop.ServiceScheduler.Core.Entities;

namespace Keyloop.ServiceScheduler.Tests;

public class AppointmentTests
{
    [Fact]
    public void OverlapsWith_WhenAppointmentsOverlap_ReturnsTrue()
    {
        var existing = CreateAppointment(
            new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 27, 11, 0, 0, TimeSpan.Zero));

        var requested = CreateAppointment(
            new DateTimeOffset(2026, 10, 27, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 27, 11, 30, 0, TimeSpan.Zero));

        Assert.True(existing.OverlapsWith(requested));
    }

    [Fact]
    public void OverlapsWith_WhenAppointmentsTouch_ReturnsFalse()
    {
        var existing = CreateAppointment(
            new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 27, 11, 0, 0, TimeSpan.Zero));

        var requested = CreateAppointment(
            new DateTimeOffset(2026, 10, 27, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));

        Assert.False(existing.OverlapsWith(requested));
    }

    [Fact]
    public void HasValidTimeRange_WhenEndIsAfterStart_ReturnsTrue()
    {
        var appointment = CreateAppointment(
            new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 27, 11, 0, 0, TimeSpan.Zero));

        Assert.True(appointment.HasValidTimeRange());
    }

    private static Appointment CreateAppointment(
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return new Appointment
        {
            StartTime = start,
            EndTime = end,
            Status = AppointmentStatus.Confirmed
        };
    }
}
