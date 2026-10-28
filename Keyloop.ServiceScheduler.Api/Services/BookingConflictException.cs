
namespace Keyloop.ServiceScheduler.Api.Services;

public sealed class BookingConflictException(string message)
    : Exception(message);
