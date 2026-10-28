
using Keyloop.ServiceScheduler.Api.Contracts;
using Keyloop.ServiceScheduler.Api.Data;
using Keyloop.ServiceScheduler.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppointmentsController(
    AppointmentBookingService bookingService,
    SchedulerDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(AppointmentResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AppointmentResponse>> Create(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var appointment = await bookingService.CreateAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = appointment.Id },
                appointment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (BookingConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException)
        {
            // Invalid service configuration is a server-side problem,
            // not a booking conflict. Avoid exposing internal details.
            return Problem(
                title: "The booking could not be completed because of a configuration error.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(AppointmentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AppointmentResponse(
                x.Id,
                x.DealershipId,
                x.VehicleId,
                x.CustomerId,
                x.ServiceTypeId,
                x.TechnicianId,
                x.ServiceBayId,
                x.StartTime,
                x.EndTime,
                x.Status))
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment is null)
        {
            return NotFound(new
            {
                error = "Appointment was not found."
            });
        }

        return Ok(appointment);
    }
}
