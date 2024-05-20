using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Flights.Data;
using Flights.ReadModels;
using Flights.Dtos;
using Flights.Domain.Errors;

namespace Flights.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class BookingController : ControllerBase
  {
    private readonly Entities _entities;
    public BookingController(Entities entities)
    {
      _entities = entities;
    }
    [HttpGet("{email}")]
    [ProducesResponseType(500)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(IEnumerable<BookingRm>), 200)]
    public ActionResult<IEnumerable<BookingRm>> List(string email)
    {
      var bookings = _entities.Flights.ToArray()//not efficent replace after sql integration
      .SelectMany(x => x.Bookings
        .Where(b => b.PassengerEmail == email)//turning every element in the collection to a BookingRm
          .Select(b => new BookingRm(
            x.Id,
            x.Airline,
            x.Price.ToString(),
            new TimePlaceRm(x.Arrival.Place, x.Arrival.Time),
            new TimePlaceRm(x.Departure.Place, x.Departure.Time),
            b.NumberOfSeats,
            email
            )));
      return Ok(bookings); //else will be null
    }

    [HttpDelete]
    [ProducesResponseType(204)]
    [ProducesResponseType(500)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public IActionResult Cancel(BookDto dto)
    {
      var flight = _entities.Flights.Find(dto.FlightId);
      var error = flight?.CancelBooking(dto.PassengerEmail, dto.NumberOfSeats);

      if(error == null) {
        _entities.SaveChanges();
        return NoContent(); //204 = success call for deletion
      }
      if (error is NotFoundError)
      {
        return NotFound();
      }
      throw new Exception($"The error of type: {error.GetType().Name} occured while canceling the booking for {dto.PassengerEmail} .");
    }
  }
}
