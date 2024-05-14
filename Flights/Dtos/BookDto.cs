namespace Flights.Dtos
{
  public record BookDto(
    Guid FlightId,
    string PassengerEmail,
    byte NumberOfSeats); //number up to 255
}
