using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BusTicketingSystem.Application.Services;
using BusTicketingSystem.Application.DTOs;

namespace BusTicketingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly ITicketingService _ticketingService;

        public StationsController(ITicketingService ticketingService)
        {
            _ticketingService = ticketingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stations = await _ticketingService.GetAllStationsAsync();
            return Ok(stations);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Station name is required.");

            var created = await _ticketingService.CreateStationAsync(dto);
            return Ok(created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _ticketingService.DeleteStationAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true, message = "Station deleted successfully." });
        }

        [HttpGet("{fromStationId}/destinations")]
        public async Task<IActionResult> GetDestinations(int fromStationId)
        {
            var destinations = await _ticketingService.GetReachableDestinationsAsync(fromStationId);
            return Ok(destinations);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly ITicketingService _ticketingService;

        public RoutesController(ITicketingService ticketingService)
        {
            _ticketingService = ticketingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var routes = await _ticketingService.GetAllRoutesAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var route = await _ticketingService.GetRouteByIdAsync(id);
            if (route == null) return NotFound();
            return Ok(route);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRouteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Stoppages == null || dto.Stoppages.Count < 2)
                return BadRequest("Route name and at least 2 stoppages are required.");

            int id = await _ticketingService.CreateRouteAsync(dto);
            return Ok(new { id, message = "Route created successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _ticketingService.DeleteRouteAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true, message = "Route deleted successfully." });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITicketingService _ticketingService;

        public TripsController(ITicketingService ticketingService)
        {
            _ticketingService = ticketingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAdmin()
        {
            var trips = await _ticketingService.GetAllTripsAdminAsync();
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto dto)
        {
            if (dto.RouteId <= 0 || string.IsNullOrWhiteSpace(dto.CoachNo) || dto.BaseFare <= 0)
                return BadRequest("Valid Route, Coach No, and Base Fare are required.");

            int id = await _ticketingService.CreateTripAsync(dto);
            return Ok(new { id, message = "Trip scheduled successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _ticketingService.DeleteTripAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true, message = "Trip deleted successfully." });
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] TripSearchRequestDto request)
        {
            var trips = await _ticketingService.SearchTripsAsync(request);
            return Ok(trips);
        }

        [HttpGet("{tripId}/layout")]
        public async Task<IActionResult> GetSeatLayout(int tripId, [FromQuery] int fromStationId, [FromQuery] int toStationId, [FromQuery] DateTime? travelDate = null)
        {
            var layout = await _ticketingService.GetTripSeatLayoutAsync(tripId, fromStationId, toStationId, travelDate);
            if (layout == null) return NotFound("Trip layout not found.");
            return Ok(layout);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly ITicketingService _ticketingService;

        public BookingsController(ITicketingService ticketingService)
        {
            _ticketingService = ticketingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _ticketingService.GetAllBookingsAsync();
            return Ok(bookings);
        }

        [HttpGet("ticket/{ticketNo}")]
        public async Task<IActionResult> GetByTicketNo(string ticketNo)
        {
            var booking = await _ticketingService.GetBookingByTicketNoAsync(ticketNo);
            if (booking == null) return NotFound(new { message = "Ticket not found." });
            return Ok(booking);
        }

        [HttpPost("issue")]
        public async Task<IActionResult> IssueTicket([FromBody] IssueTicketRequestDto request)
        {
            var result = await _ticketingService.IssueTicketAsync(request);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
