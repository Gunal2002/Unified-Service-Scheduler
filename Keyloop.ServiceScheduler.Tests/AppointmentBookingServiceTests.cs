
using Keyloop.ServiceScheduler.Api.Contracts;
using Keyloop.ServiceScheduler.Api.Data;
using Keyloop.ServiceScheduler.Api.Services;
using Keyloop.ServiceScheduler.Core.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Keyloop.ServiceScheduler.Tests;

public class AppointmentBookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WithAvailableResources_CreatesAppointment()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var result = await service.CreateAsync(setup.Request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(AppointmentStatus.Confirmed, result.Status);
        Assert.NotEqual(Guid.Empty, result.TechnicianId);
        Assert.NotEqual(Guid.Empty, result.ServiceBayId);
        Assert.Equal(
            setup.Request.StartTime.AddMinutes(60).ToUniversalTime(),
            result.EndTime);

        Assert.Equal(1, await setup.Db.Appointments.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenTechnicianIsAlreadyBooked_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        await service.CreateAsync(setup.Request);

        var exception =
            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.CreateAsync(setup.Request));

        Assert.Contains(
            "No qualified technician is available",
            exception.Message);

        Assert.Equal(1, await setup.Db.Appointments.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenNoTechnicianHasRequiredSkill_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync(
            technicianQualified: false);

        var service = new AppointmentBookingService(setup.Db);

        var exception =
            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.CreateAsync(setup.Request));

        Assert.Contains(
            "No qualified technician is available",
            exception.Message);

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenNoActiveBayExists_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync(
            activeBay: false);

        var service = new AppointmentBookingService(setup.Db);

        var exception =
            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.CreateAsync(setup.Request));

        Assert.Contains(
            "No service bay is available",
            exception.Message);

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenBayIsOccupiedButAnotherTechnicianIsFree_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync(
            addSecondTechnician: true);

        var service = new AppointmentBookingService(setup.Db);

        var first = await service.CreateAsync(setup.Request);

        var secondRequest = CopyRequest(
            setup.Request,
            setup.Request.StartTime);

        var exception =
            await Assert.ThrowsAsync<BookingConflictException>(
                () => service.CreateAsync(secondRequest));

        Assert.Contains("No service bay is available", exception.Message);
        Assert.Equal(1, await setup.Db.Appointments.CountAsync());
        Assert.NotEqual(Guid.Empty, first.ServiceBayId);
    }

    [Fact]
    public async Task CreateAsync_WhenStartTimeIsInThePast_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var request = CopyRequest(
            setup.Request,
            DateTimeOffset.UtcNow.AddHours(-1));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenStartTimeIsMissing_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var request = CopyRequest(setup.Request, default);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenVehicleBelongsToAnotherCustomer_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var request = CopyRequest(
            setup.Request,
            setup.Request.StartTime);

        request.CustomerId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenDealershipDoesNotExist_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var request = CopyRequest(
            setup.Request,
            setup.Request.StartTime);

        request.DealershipId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenServiceDoesNotBelongToDealership_RejectsBooking()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var request = CopyRequest(
            setup.Request,
            setup.Request.StartTime);

        request.DealershipId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(request));

        Assert.Empty(await setup.Db.Appointments.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenAppointmentsAreAdjacent_AllowsBothBookings()
    {
        await using var setup = await CreateSeededDatabaseAsync();

        var service = new AppointmentBookingService(setup.Db);

        var first = await service.CreateAsync(setup.Request);

        var secondRequest = CopyRequest(
            setup.Request,
            first.EndTime);

        var second = await service.CreateAsync(secondRequest);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.EndTime, second.StartTime);
        Assert.Equal(2, await setup.Db.Appointments.CountAsync());
    }

    private static CreateAppointmentRequest CopyRequest(
        CreateAppointmentRequest original,
        DateTimeOffset startTime)
    {
        return new CreateAppointmentRequest
        {
            DealershipId = original.DealershipId,
            CustomerId = original.CustomerId,
            VehicleId = original.VehicleId,
            ServiceTypeId = original.ServiceTypeId,
            StartTime = startTime
        };
    }

    private static async Task<TestDatabase> CreateSeededDatabaseAsync(
        bool technicianQualified = true,
        bool activeBay = true,
        bool addSecondTechnician = false)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new SchedulerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var dealership = new Dealership
        {
            Name = "Test Dealership",
            TimeZoneId = "UTC"
        };

        var customer = new Customer
        {
            Name = "Test Customer",
            Email = "test@example.com"
        };

        var vehicle = new Vehicle
        {
            CustomerId = customer.Id,
            RegistrationNumber = "TEST-123",
            Make = "Toyota",
            Model = "Corolla"
        };

        var serviceType = new ServiceType
        {
            DealershipId = dealership.Id,
            Name = "Oil Service",
            DurationMinutes = 60,
            RequiredSkill = "GeneralMaintenance"
        };

        var technician = new Technician
        {
            DealershipId = dealership.Id,
            Name = "Test Technician",
            Skills = technicianQualified
                ? ["GeneralMaintenance"]
                : ["BodyRepair"]
        };

        var bay = new ServiceBay
        {
            DealershipId = dealership.Id,
            Name = "Bay 1",
            IsActive = activeBay
        };

        db.AddRange(
            dealership,
            customer,
            vehicle,
            serviceType,
            technician,
            bay);

        if (addSecondTechnician)
        {
            db.Technicians.Add(new Technician
            {
                DealershipId = dealership.Id,
                Name = "Second Technician",
                Skills = ["GeneralMaintenance"]
            });
        }

        await db.SaveChangesAsync();

        var request = new CreateAppointmentRequest
        {
            DealershipId = dealership.Id,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            ServiceTypeId = serviceType.Id,
            StartTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        return new TestDatabase(connection, db, request);
    }

    private sealed class TestDatabase(
        SqliteConnection connection,
        SchedulerDbContext db,
        CreateAppointmentRequest request) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;

        public SchedulerDbContext Db { get; } = db;

        public CreateAppointmentRequest Request { get; } = request;

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
