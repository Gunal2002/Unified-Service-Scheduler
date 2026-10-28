using Keyloop.ServiceScheduler.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Keyloop.ServiceScheduler.Api.Data
{
    public sealed class SchedulerDbContext(
    DbContextOptions<SchedulerDbContext> options)
    : DbContext(options)
    {
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Dealership> Dealerships => Set<Dealership>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
        public DbSet<Technician> Technicians => Set<Technician>();
        public DbSet<ServiceBay> ServiceBays => Set<ServiceBay>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new
                {
                    x.TechnicianId,
                    x.StartTime,
                    x.EndTime
                });

                entity.HasIndex(x => new
                {
                    x.ServiceBayId,
                    x.StartTime,
                    x.EndTime
                });
            });

            modelBuilder.Entity<Customer>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Dealership>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Vehicle>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<ServiceType>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Technician>()
                .Property(x => x.Skills)
                .HasConversion(
                    skills => string.Join("|", skills),
                    value => value.Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(
                    new ValueComparer<List<string>>(
                        (left, right) => left != null &&
                                         right != null &&
                                         left.SequenceEqual(right),
                        value => value.Aggregate(
                            0,
                            (hash, skill) =>
                                HashCode.Combine(hash, skill.GetHashCode())),
                        value => value.ToList()));

            modelBuilder.Entity<ServiceBay>()
                .HasKey(x => x.Id);

        }
    }
}
