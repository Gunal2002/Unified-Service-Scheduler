
using Keyloop.ServiceScheduler.Api.Contracts;
using Keyloop.ServiceScheduler.Api.Data;
using Keyloop.ServiceScheduler.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.ServiceScheduler.Api.Services;

public sealed class AppointmentBookingService(
    SchedulerDbContext dbContext)
{
    // Serializes booking requests within this API process.
    // This is not a distributed concurrency guarantee.
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    public async Task<AppointmentResponse> CreateAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await BookingLock.WaitAsync(cancellationToken);

        try
        {
            return await CreateCoreAsync(request, cancellationToken);
        }
        finally
        {
            BookingLock.Release();
        }
    }

    private async Task<AppointmentResponse> CreateCoreAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DealershipId == Guid.Empty)
            throw new ArgumentException("Dealership ID is required.");

        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.");

        if (request.VehicleId == Guid.Empty)
            throw new ArgumentException("Vehicle ID is required.");

        if (request.ServiceTypeId == Guid.Empty)
            throw new ArgumentException("Service type ID is required.");

        if (request.StartTime == default)
            throw new ArgumentException("Start time is required.");

        var startTime = request.StartTime.ToUniversalTime();

        if (startTime <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Appointment must be scheduled in the future.");
        }

        var dealershipExists = await dbContext.Dealerships
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == request.DealershipId,
                cancellationToken);

        if (!dealershipExists)
            throw new ArgumentException("Dealership was not found.");

        var customerExists = await dbContext.Customers
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == request.CustomerId,
                cancellationToken);

        if (!customerExists)
            throw new ArgumentException("Customer was not found.");

        var vehicleExists = await dbContext.Vehicles
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == request.VehicleId &&
                     x.CustomerId == request.CustomerId,
                cancellationToken);

        if (!vehicleExists)
        {
            throw new ArgumentException(
                "Vehicle was not found for this customer.");
        }

        var service = await dbContext.ServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.ServiceTypeId &&
                     x.DealershipId == request.DealershipId,
                cancellationToken);

        if (service is null)
        {
            throw new ArgumentException(
                "Service type was not found for this dealership.");
        }

        if (service.DurationMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Service duration must be greater than zero.");
        }

        DateTimeOffset endTime;

        try
        {
            endTime = startTime.AddMinutes(service.DurationMinutes);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentException(
                "The requested appointment time is out of range.");
        }

        // Fetch resources belonging to the requested dealership.
        var technicians = await dbContext.Technicians
            .AsNoTracking()
            .Where(x => x.DealershipId == request.DealershipId)
            .ToListAsync(cancellationToken);

        var bays = await dbContext.ServiceBays
            .AsNoTracking()
            .Where(x => x.DealershipId == request.DealershipId &&
                        x.IsActive)
            .ToListAsync(cancellationToken);

        // SQLite DateTimeOffset range queries can be provider-sensitive.
        // Load confirmed appointments and compare normalized times in memory.
        var existingAppointments = await dbContext.Appointments
            .AsNoTracking()
            .Where(x =>
                x.DealershipId == request.DealershipId &&
                x.Status == AppointmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var qualifiedTechnicians = technicians
            .Where(t => t.Skills.Any(skill =>
                string.Equals(
                    skill,
                    service.RequiredSkill,
                    StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var availableTechnician = qualifiedTechnicians.FirstOrDefault(t =>
            !existingAppointments.Any(a =>
                a.TechnicianId == t.Id &&
                startTime < a.EndTime &&
                endTime > a.StartTime));

        if (availableTechnician is null)
        {
            throw new BookingConflictException(
                "No qualified technician is available for this time.");
        }

        var availableBay = bays.FirstOrDefault(b =>
            !existingAppointments.Any(a =>
                a.ServiceBayId == b.Id &&
                startTime < a.EndTime &&
                endTime > a.StartTime));

        if (availableBay is null)
        {
            throw new BookingConflictException(
                "No service bay is available for this time.");
        }

        var appointment = new Appointment
        {
            DealershipId = request.DealershipId,
            VehicleId = request.VehicleId,
            CustomerId = request.CustomerId,
            ServiceTypeId = request.ServiceTypeId,
            TechnicianId = availableTechnician.Id,
            ServiceBayId = availableBay.Id,
            StartTime = startTime,
            EndTime = endTime,
            Status = AppointmentStatus.Confirmed
        };

        dbContext.Appointments.Add(appointment);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AppointmentResponse(
            appointment.Id,
            appointment.DealershipId,
            appointment.VehicleId,
            appointment.CustomerId,
            appointment.ServiceTypeId,
            appointment.TechnicianId,
            appointment.ServiceBayId,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Status);
    }
}
