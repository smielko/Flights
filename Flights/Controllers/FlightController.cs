using Flights.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Flights.Dtos;
using Flights.Domain.Entities;
using Flights.Domain.Errors;
using Flights.Data;
namespace Flights.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class FlightController : ControllerBase
  {
    private readonly ILogger<FlightController> _logger;
    private readonly Entities _entities; //gets instantiated in the constructor (ctor)
    public FlightController(ILogger<FlightController> logger,
      Entities entities) //instance of singleton from the Program.cs
    {
      _logger = logger;
      _entities = entities;
    }

    [HttpGet]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [ProducesResponseType(typeof(IEnumerable<FlightRm>), 200)]
    public IEnumerable<FlightRm> Search()
    {
      var flightRmList = _entities.Flights.Select(flight => new FlightRm(
              flight.Id,
              flight.Airline,
              flight.Price,
              new TimePlaceRm(flight.Departure.Place.ToString(), flight.Departure.Time),
              new TimePlaceRm(flight.Arrival.Place.ToString(), flight.Arrival.Time),
              flight.RemainingNumberOfSeats
              ));
      return flightRmList;
    }


    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [ProducesResponseType(typeof(FlightRm), 200)]
    public ActionResult<FlightRm> Find(Guid id)
    {
      var flight = _entities.Flights.SingleOrDefault(f => f.Id == id);

      if (flight == null)
        return NotFound();
      var readModel = new FlightRm(
        flight.Id,
        flight.Airline,
        flight.Price,
        new TimePlaceRm(flight.Departure.Place.ToString(), flight.Departure.Time),
        new TimePlaceRm(flight.Arrival.Place.ToString(), flight.Arrival.Time),
        flight.RemainingNumberOfSeats
        );
      return Ok(readModel);
    }

    [HttpPost]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public IActionResult Book(BookDto dto)
    {
      System.Diagnostics.Debug.WriteLine($"Booking at flight {dto.FlightId}");

      var flight = _entities.Flights.SingleOrDefault(f => f.Id == dto.FlightId);

      if (flight == null)
      {
        return NotFound();
      }

      var error = flight.MakeBooking(dto.PassengerEmail, dto.NumberOfSeats);
      if (error is OverbookError) {
       return  Conflict(new { message = " The number of Requested seats exceeds the seats remaining." });
      }
      return CreatedAtAction(nameof(Find), new { id = dto.FlightId });
    }
  }
}
