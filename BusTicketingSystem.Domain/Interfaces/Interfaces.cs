using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusTicketingSystem.Domain.Entities;

namespace BusTicketingSystem.Domain.Interfaces
{
    public interface IStationRepository
    {
        Task<Station?> GetByIdAsync(int id);
        Task<Station?> GetByCodeAsync(string code);
        Task<IEnumerable<Station>> GetAllAsync();
        Task<int> AddAsync(Station station);
        Task<bool> DeleteAsync(int id);
    }

    public interface IRouteRepository
    {
        Task<Route?> GetByIdAsync(int id);
        Task<Route?> GetRouteWithStoppagesAsync(int routeId);
        Task<IEnumerable<Route>> GetAllRoutesWithStoppagesAsync();
        Task<IEnumerable<Route>> GetActiveRoutesAsync();
        Task<int> AddRouteWithStoppagesAsync(Route route);
        Task<bool> DeleteRouteAsync(int id);
    }

    public interface ITripRepository
    {
        Task<Trip?> GetByIdAsync(int id);
        Task<Trip?> GetTripWithDetailsAsync(int tripId);
        Task<IEnumerable<Trip>> GetAllTripsAsync();
        Task<IEnumerable<Trip>> SearchTripsAsync(int fromStationId, int toStationId, DateTime date);
        Task<int> AddAsync(Trip trip);
        Task<bool> DeleteAsync(int id);
    }

    public interface IBookingRepository
    {
        Task<IEnumerable<BookingSeat>> GetOccupiedSeatsForTripAsync(int tripId, int startSeq, int endSeq, DateTime? travelDate = null);
        Task<Booking?> GetByTicketNoAsync(string ticketNo);
        Task<IEnumerable<Booking>> GetAllBookingsAsync();
        Task<int> AddBookingAsync(Booking booking);
    }

    public interface IUnitOfWork : IDisposable
    {
        IStationRepository Stations { get; }
        IRouteRepository Routes { get; }
        ITripRepository Trips { get; }
        IBookingRepository Bookings { get; }
    }
}
