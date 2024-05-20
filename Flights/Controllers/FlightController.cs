using Flights.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Flights.Dtos;
using Flights.Domain.Entities;
using Flights.Domain.Errors;
using Flights.Data;
using Microsoft.EntityFrameworkCore;
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
    public IEnumerable<FlightRm> Search([FromQuery] FlightSearchParameters @params)
    {
      _logger.LogInformation("Searching for a flight destination: {Destination}",@params.Destination);

      IQueryable<Flight> flights = _entities.Flights;
      if (!string.IsNullOrWhiteSpace(@params.Destination))
      {
        flights = flights.Where(f => f.Arrival.Place.Contains(@params.Destination));
      }

      if (!string.IsNullOrWhiteSpace(@params.From))
      {
        flights = flights.Where(f => f.Departure.Place.Contains(@params.From));
      }

      if (@params.FromDate != null)
      {
        flights = flights.Where(f => f.Departure.Time >= @params.FromDate.Value.Date);
      }

      if (@params.ToDate != null)
      {
        flights = flights.Where(f => f.Departure.Time >= @params.ToDate.Value.Date.AddDays(1).AddTicks(-1));
      }

      if (@params.NumberOfPassengers != null && @params.NumberOfPassengers != 0)
      {
        flights = flights.Where(f => f.RemainingNumberOfSeats >= @params.NumberOfPassengers);
      }

      else
      {
        flights = flights.Where(f => f.RemainingNumberOfSeats >= 1);
      }

      var flightRmList = flights
          .Select(flight => new FlightRm(
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

      
      try {
        _entities.SaveChanges(); //!!!!!
      } catch(DbUpdateConcurrencyException)
      {
        return Conflict(new { message = "An error occured while booking, please try again." });
      }
      
        
      
        return CreatedAtAction(nameof(Find), new { id = dto.FlightId });
    }
  }
}
