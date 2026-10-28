
using Keyloop.ServiceScheduler.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.ServiceScheduler.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        SchedulerDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (await db.Dealerships.AnyAsync(cancellationToken))
            return;

        var dealership = new Dealership
        {
            Name = "Keyloop Demo Dealership",
            TimeZoneId = "UTC"
        };

        var customer = new Customer
        {
            Name = "Alex Morgan",
            Email = "alex@example.com"
        };

        var vehicle = new Vehicle
        {
            CustomerId = customer.Id,
            RegistrationNumber = "DEMO-123",
            Make = "Toyota",
            Model = "Corolla"
        };

        var service = new ServiceType
        {
            DealershipId = dealership.Id,
            Name = "Engine Oil Service",
            DurationMinutes = 60,
            RequiredSkill = "GeneralMaintenance"
        };

        var technician = new Technician
        {
            DealershipId = dealership.Id,
            Name = "Jordan Lee",
            Skills = ["GeneralMaintenance"]
        };

        var bay = new ServiceBay
        {
            DealershipId = dealership.Id,
            Name = "Bay 1",
            IsActive = true
        };

        db.Dealerships.Add(dealership);
        db.Customers.Add(customer);
        db.Vehicles.Add(vehicle);
        db.ServiceTypes.Add(service);
        db.Technicians.Add(technician);
        db.ServiceBays.Add(bay);

        await db.SaveChangesAsync(cancellationToken);
    }
}
