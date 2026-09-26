using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using BusTicketingSystem.Domain.Entities;
using BusTicketingSystem.Domain.Interfaces;
using BusTicketingSystem.Infrastructure.Data;

namespace BusTicketingSystem.Infrastructure.Repositories
{
    public class StationAdoRepository : IStationRepository
    {
        private readonly IDbConnectionFactory _db;

        public StationAdoRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Station>> GetAllAsync()
        {
            var list = new List<Station>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "SELECT Id, Name, District, CounterCode FROM Stations ORDER BY Id ASC;";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Station
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    District = reader.GetString(2),
                    CounterCode = reader.GetString(3)
                });
            }

            return list;
        }

        public async Task<Station?> GetByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "SELECT Id, Name, District, CounterCode FROM Stations WHERE Id = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Station
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    District = reader.GetString(2),
                    CounterCode = reader.GetString(3)
                };
            }

            return null;
        }

        public async Task<Station?> GetByCodeAsync(string code)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "SELECT Id, Name, District, CounterCode FROM Stations WHERE CounterCode = @Code;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Code", code);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Station
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    District = reader.GetString(2),
                    CounterCode = reader.GetString(3)
                };
            }

            return null;
        }

        public async Task<int> AddAsync(Station station)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO Stations (Name, District, CounterCode)
                OUTPUT INSERTED.Id
                VALUES (@Name, @District, @CounterCode);
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", station.Name);
            cmd.Parameters.AddWithValue("@District", station.District);
            cmd.Parameters.AddWithValue("@CounterCode", station.CounterCode);

            var id = await cmd.ExecuteScalarAsync();
            station.Id = Convert.ToInt32(id);
            return station.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "DELETE FROM Stations WHERE Id = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }

    public class RouteAdoRepository : IRouteRepository
    {
        private readonly IDbConnectionFactory _db;

        public RouteAdoRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<Route?> GetByIdAsync(int id)
        {
            return await GetRouteWithStoppagesAsync(id);
        }

        public async Task<Route?> GetRouteWithStoppagesAsync(int routeId)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    r.Id, r.Name, r.ViaName, r.IsActive,
                    rs.Id AS StoppageId, rs.StationId, rs.StopSequence, rs.DistanceFromStartKm, rs.BaseFareFromStart,
                    s.Name AS StationName, s.District, s.CounterCode
                FROM Routes r
                LEFT JOIN RouteStoppages rs ON r.Id = rs.RouteId
                LEFT JOIN Stations s ON rs.StationId = s.Id
                WHERE r.Id = @RouteId
                ORDER BY rs.StopSequence ASC;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RouteId", routeId);
            using var reader = await cmd.ExecuteReaderAsync();

            Route? route = null;

            while (await reader.ReadAsync())
            {
                if (route == null)
                {
                    route = new Route
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        ViaName = reader.GetString(2),
                        IsActive = reader.GetBoolean(3)
                    };
                }

                if (!reader.IsDBNull(4))
                {
                    route.Stoppages.Add(new RouteStoppage
                    {
                        Id = reader.GetInt32(4),
                        RouteId = route.Id,
                        StationId = reader.GetInt32(5),
                        StopSequence = reader.GetInt32(6),
                        DistanceFromStartKm = reader.GetDecimal(7),
                        BaseFareFromStart = reader.GetDecimal(8),
                        Station = new Station
                        {
                            Id = reader.GetInt32(5),
                            Name = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            District = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            CounterCode = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                        }
                    });
                }
            }

            return route;
        }

        public async Task<IEnumerable<Route>> GetAllRoutesWithStoppagesAsync()
        {
            var routesMap = new Dictionary<int, Route>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    r.Id, r.Name, r.ViaName, r.IsActive,
                    rs.Id AS StoppageId, rs.StationId, rs.StopSequence, rs.DistanceFromStartKm, rs.BaseFareFromStart,
                    s.Name AS StationName, s.District, s.CounterCode
                FROM Routes r
                LEFT JOIN RouteStoppages rs ON r.Id = rs.RouteId
                LEFT JOIN Stations s ON rs.StationId = s.Id
                ORDER BY r.Id ASC, rs.StopSequence ASC;
            ";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int routeId = reader.GetInt32(0);
                if (!routesMap.TryGetValue(routeId, out var route))
                {
                    route = new Route
                    {
                        Id = routeId,
                        Name = reader.GetString(1),
                        ViaName = reader.GetString(2),
                        IsActive = reader.GetBoolean(3)
                    };
                    routesMap[routeId] = route;
                }

                if (!reader.IsDBNull(4))
                {
                    route.Stoppages.Add(new RouteStoppage
                    {
                        Id = reader.GetInt32(4),
                        RouteId = routeId,
                        StationId = reader.GetInt32(5),
                        StopSequence = reader.GetInt32(6),
                        DistanceFromStartKm = reader.GetDecimal(7),
                        BaseFareFromStart = reader.GetDecimal(8),
                        Station = new Station
                        {
                            Id = reader.GetInt32(5),
                            Name = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            District = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            CounterCode = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                        }
                    });
                }
            }

            return routesMap.Values;
        }

        public async Task<IEnumerable<Route>> GetActiveRoutesAsync()
        {
            var all = await GetAllRoutesWithStoppagesAsync();
            return all.Where(r => r.IsActive);
        }

        public async Task<int> AddRouteWithStoppagesAsync(Route route)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                string insertRouteSql = @"
                    INSERT INTO Routes (Name, ViaName, IsActive)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @ViaName, @IsActive);
                ";

                using var cmdRoute = new SqlCommand(insertRouteSql, conn, tx);
                cmdRoute.Parameters.AddWithValue("@Name", route.Name);
                cmdRoute.Parameters.AddWithValue("@ViaName", route.ViaName);
                cmdRoute.Parameters.AddWithValue("@IsActive", route.IsActive);

                var routeIdObj = await cmdRoute.ExecuteScalarAsync();
                int routeId = Convert.ToInt32(routeIdObj);
                route.Id = routeId;

                foreach (var stoppage in route.Stoppages)
                {
                    string insertStoppageSql = @"
                        INSERT INTO RouteStoppages (RouteId, StationId, StopSequence, DistanceFromStartKm, BaseFareFromStart)
                        VALUES (@RouteId, @StationId, @StopSequence, @DistanceFromStartKm, @BaseFareFromStart);
                    ";

                    using var cmdStop = new SqlCommand(insertStoppageSql, conn, tx);
                    cmdStop.Parameters.AddWithValue("@RouteId", routeId);
                    cmdStop.Parameters.AddWithValue("@StationId", stoppage.StationId);
                    cmdStop.Parameters.AddWithValue("@StopSequence", stoppage.StopSequence);
                    cmdStop.Parameters.AddWithValue("@DistanceFromStartKm", stoppage.DistanceFromStartKm);
                    cmdStop.Parameters.AddWithValue("@BaseFareFromStart", stoppage.BaseFareFromStart);

                    await cmdStop.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return routeId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "DELETE FROM Routes WHERE Id = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }

    public class TripAdoRepository : ITripRepository
    {
        private readonly IDbConnectionFactory _db;
        private readonly IRouteRepository _routeRepo;

        public TripAdoRepository(IDbConnectionFactory db, IRouteRepository routeRepo)
        {
            _db = db;
            _routeRepo = routeRepo;
        }

        public async Task<Trip?> GetByIdAsync(int id)
        {
            return await GetTripWithDetailsAsync(id);
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync()
        {
            var trips = new List<Trip>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    t.Id, t.RouteId, t.CoachNo, t.CoachType, t.RegistrationNo, 
                    t.DepartureTime, t.StartingCounter, t.EndCounter, t.BaseFare, t.TotalSeats,
                    r.Name AS RouteName, r.ViaName
                FROM Trips t
                LEFT JOIN Routes r ON t.RouteId = r.Id
                ORDER BY t.DepartureTime ASC, t.Id ASC;
            ";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                trips.Add(new Trip
                {
                    Id = reader.GetInt32(0),
                    RouteId = reader.GetInt32(1),
                    CoachNo = reader.GetString(2),
                    CoachType = reader.GetString(3),
                    RegistrationNo = reader.GetString(4),
                    DepartureTime = reader.GetDateTime(5),
                    StartingCounter = reader.GetString(6),
                    EndCounter = reader.GetString(7),
                    BaseFare = reader.GetDecimal(8),
                    TotalSeats = reader.GetInt32(9),
                    Route = new Route
                    {
                        Id = reader.GetInt32(1),
                        Name = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                        ViaName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                    }
                });
            }

            return trips;
        }

        public async Task<Trip?> GetTripWithDetailsAsync(int tripId)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    t.Id, t.RouteId, t.CoachNo, t.CoachType, t.RegistrationNo, 
                    t.DepartureTime, t.StartingCounter, t.EndCounter, t.BaseFare, t.TotalSeats
                FROM Trips t
                WHERE t.Id = @TripId;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TripId", tripId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            var trip = new Trip
            {
                Id = reader.GetInt32(0),
                RouteId = reader.GetInt32(1),
                CoachNo = reader.GetString(2),
                CoachType = reader.GetString(3),
                RegistrationNo = reader.GetString(4),
                DepartureTime = reader.GetDateTime(5),
                StartingCounter = reader.GetString(6),
                EndCounter = reader.GetString(7),
                BaseFare = reader.GetDecimal(8),
                TotalSeats = reader.GetInt32(9)
            };

            reader.Close();

            // Load Route with Stoppages
            trip.Route = await _routeRepo.GetRouteWithStoppagesAsync(trip.RouteId);
            return trip;
        }

        public async Task<IEnumerable<Trip>> SearchTripsAsync(int fromStationId, int toStationId, DateTime date)
        {
            var allTrips = await GetAllTripsAsync();
            var allRoutes = await _routeRepo.GetAllRoutesWithStoppagesAsync();
            var routesDict = allRoutes.ToDictionary(r => r.Id, r => r);

            var matchedTrips = new List<Trip>();

            foreach (var trip in allTrips)
            {
                if (routesDict.TryGetValue(trip.RouteId, out var route))
                {
                    trip.Route = route;
                    var stoppages = route.Stoppages.OrderBy(s => s.StopSequence).ToList();
                    var fromStop = stoppages.FirstOrDefault(s => s.StationId == fromStationId);
                    var toStop = stoppages.FirstOrDefault(s => s.StationId == toStationId);

                    if (fromStop != null && toStop != null && fromStop.StopSequence < toStop.StopSequence)
                    {
                        matchedTrips.Add(trip);
                    }
                }
            }

            return matchedTrips;
        }

        public async Task<int> AddAsync(Trip trip)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO Trips (RouteId, CoachNo, CoachType, RegistrationNo, DepartureTime, StartingCounter, EndCounter, BaseFare, TotalSeats)
                OUTPUT INSERTED.Id
                VALUES (@RouteId, @CoachNo, @CoachType, @RegistrationNo, @DepartureTime, @StartingCounter, @EndCounter, @BaseFare, @TotalSeats);
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RouteId", trip.RouteId);
            cmd.Parameters.AddWithValue("@CoachNo", trip.CoachNo);
            cmd.Parameters.AddWithValue("@CoachType", trip.CoachType);
            cmd.Parameters.AddWithValue("@RegistrationNo", trip.RegistrationNo);
            cmd.Parameters.AddWithValue("@DepartureTime", trip.DepartureTime);
            cmd.Parameters.AddWithValue("@StartingCounter", trip.StartingCounter);
            cmd.Parameters.AddWithValue("@EndCounter", trip.EndCounter);
            cmd.Parameters.AddWithValue("@BaseFare", trip.BaseFare);
            cmd.Parameters.AddWithValue("@TotalSeats", trip.TotalSeats);

            var id = await cmd.ExecuteScalarAsync();
            trip.Id = Convert.ToInt32(id);
            return trip.Id;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = "DELETE FROM Trips WHERE Id = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }

    public class BookingAdoRepository : IBookingRepository
    {
        private readonly IDbConnectionFactory _db;

        public BookingAdoRepository(IDbConnectionFactory db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BookingSeat>> GetOccupiedSeatsForTripAsync(int tripId, int startSeq, int endSeq, DateTime? travelDate = null)
        {
            var list = new List<BookingSeat>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string dateFilter = travelDate.HasValue ? " AND CAST(b.IssuedAt AS DATE) = CAST(@TravelDate AS DATE)" : "";

            string sql = $@"
                SELECT 
                    bs.Id, bs.BookingId, bs.SeatNo, bs.StartSequence, bs.EndSequence,
                    b.TripId, b.PassengerName, b.Mobile, b.Gender, b.Status, b.TicketNo
                FROM BookingSeats bs
                INNER JOIN Bookings b ON bs.BookingId = b.Id
                WHERE b.TripId = @TripId 
                  AND b.Status <> 'Cancelled'
                  {dateFilter}
                  AND bs.StartSequence < @EndSeq 
                  AND bs.EndSequence > @StartSeq;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TripId", tripId);
            cmd.Parameters.AddWithValue("@StartSeq", startSeq);
            cmd.Parameters.AddWithValue("@EndSeq", endSeq);
            if (travelDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@TravelDate", travelDate.Value.Date);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new BookingSeat
                {
                    Id = reader.GetInt32(0),
                    BookingId = reader.GetInt32(1),
                    SeatNo = reader.GetString(2),
                    StartSequence = reader.GetInt32(3),
                    EndSequence = reader.GetInt32(4),
                    Booking = new Booking
                    {
                        Id = reader.GetInt32(1),
                        TripId = reader.GetInt32(5),
                        PassengerName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        Mobile = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        Gender = reader.IsDBNull(8) ? "Male" : reader.GetString(8),
                        Status = reader.IsDBNull(9) ? "Issued" : reader.GetString(9),
                        TicketNo = reader.IsDBNull(10) ? string.Empty : reader.GetString(10)
                    }
                });
            }

            return list;
        }

        public async Task<Booking?> GetByTicketNoAsync(string ticketNo)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    b.Id, b.TicketNo, b.TripId, b.PassengerName, b.Mobile, b.Gender, b.Age,
                    b.Address, b.PassportNo, b.BoardingStationId, b.DroppingStationId,
                    b.BoardingSequence, b.DroppingSequence, b.GrossPay, b.Discount, b.NetPay,
                    b.PaymentMethod, b.Status, b.IssuedAt,
                    bs.Id AS SeatId, bs.SeatNo, bs.StartSequence, bs.EndSequence
                FROM Bookings b
                LEFT JOIN BookingSeats bs ON b.Id = bs.BookingId
                WHERE b.TicketNo = @TicketNo;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TicketNo", ticketNo);
            using var reader = await cmd.ExecuteReaderAsync();

            Booking? booking = null;

            while (await reader.ReadAsync())
            {
                if (booking == null)
                {
                    booking = new Booking
                    {
                        Id = reader.GetInt32(0),
                        TicketNo = reader.GetString(1),
                        TripId = reader.GetInt32(2),
                        PassengerName = reader.GetString(3),
                        Mobile = reader.GetString(4),
                        Gender = reader.GetString(5),
                        Age = reader.GetInt32(6),
                        Address = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        PassportNo = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        BoardingStationId = reader.GetInt32(9),
                        DroppingStationId = reader.GetInt32(10),
                        BoardingSequence = reader.GetInt32(11),
                        DroppingSequence = reader.GetInt32(12),
                        GrossPay = reader.GetDecimal(13),
                        Discount = reader.GetDecimal(14),
                        NetPay = reader.GetDecimal(15),
                        PaymentMethod = reader.GetString(16),
                        Status = reader.GetString(17),
                        IssuedAt = reader.GetDateTime(18)
                    };
                }

                if (!reader.IsDBNull(19))
                {
                    booking.Seats.Add(new BookingSeat
                    {
                        Id = reader.GetInt32(19),
                        BookingId = booking.Id,
                        SeatNo = reader.GetString(20),
                        StartSequence = reader.GetInt32(21),
                        EndSequence = reader.GetInt32(22)
                    });
                }
            }

            return booking;
        }

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
        {
            var bookingsMap = new Dictionary<int, Booking>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    b.Id, b.TicketNo, b.TripId, b.PassengerName, b.Mobile, b.Gender, b.Age,
                    b.Address, b.PassportNo, b.BoardingStationId, b.DroppingStationId,
                    b.BoardingSequence, b.DroppingSequence, b.GrossPay, b.Discount, b.NetPay,
                    b.PaymentMethod, b.Status, b.IssuedAt,
                    bs.Id AS SeatId, bs.SeatNo, bs.StartSequence, bs.EndSequence,
                    t.CoachNo
                FROM Bookings b
                LEFT JOIN BookingSeats bs ON b.Id = bs.BookingId
                LEFT JOIN Trips t ON b.TripId = t.Id
                ORDER BY b.IssuedAt DESC;
            ";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int bookingId = reader.GetInt32(0);
                if (!bookingsMap.TryGetValue(bookingId, out var booking))
                {
                    booking = new Booking
                    {
                        Id = bookingId,
                        TicketNo = reader.GetString(1),
                        TripId = reader.GetInt32(2),
                        PassengerName = reader.GetString(3),
                        Mobile = reader.GetString(4),
                        Gender = reader.GetString(5),
                        Age = reader.GetInt32(6),
                        Address = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        PassportNo = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        BoardingStationId = reader.GetInt32(9),
                        DroppingStationId = reader.GetInt32(10),
                        BoardingSequence = reader.GetInt32(11),
                        DroppingSequence = reader.GetInt32(12),
                        GrossPay = reader.GetDecimal(13),
                        Discount = reader.GetDecimal(14),
                        NetPay = reader.GetDecimal(15),
                        PaymentMethod = reader.GetString(16),
                        Status = reader.GetString(17),
                        IssuedAt = reader.GetDateTime(18),
                        Trip = new Trip
                        {
                            Id = reader.GetInt32(2),
                            CoachNo = reader.IsDBNull(23) ? string.Empty : reader.GetString(23)
                        }
                    };
                    bookingsMap[bookingId] = booking;
                }

                if (!reader.IsDBNull(19))
                {
                    booking.Seats.Add(new BookingSeat
                    {
                        Id = reader.GetInt32(19),
                        BookingId = bookingId,
                        SeatNo = reader.GetString(20),
                        StartSequence = reader.GetInt32(21),
                        EndSequence = reader.GetInt32(22)
                    });
                }
            }

            return bookingsMap.Values;
        }

        public async Task<int> AddBookingAsync(Booking booking)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                string insertBookingSql = @"
                    INSERT INTO Bookings (
                        TicketNo, TripId, PassengerName, Mobile, Gender, Age, Address, PassportNo,
                        BoardingStationId, DroppingStationId, BoardingSequence, DroppingSequence,
                        GrossPay, Discount, NetPay, PaymentMethod, Status, IssuedAt
                    )
                    OUTPUT INSERTED.Id
                    VALUES (
                        @TicketNo, @TripId, @PassengerName, @Mobile, @Gender, @Age, @Address, @PassportNo,
                        @BoardingStationId, @DroppingStationId, @BoardingSequence, @DroppingSequence,
                        @GrossPay, @Discount, @NetPay, @PaymentMethod, @Status, @IssuedAt
                    );
                ";

                using var cmdBooking = new SqlCommand(insertBookingSql, conn, tx);
                cmdBooking.Parameters.AddWithValue("@TicketNo", booking.TicketNo);
                cmdBooking.Parameters.AddWithValue("@TripId", booking.TripId);
                cmdBooking.Parameters.AddWithValue("@PassengerName", booking.PassengerName);
                cmdBooking.Parameters.AddWithValue("@Mobile", booking.Mobile);
                cmdBooking.Parameters.AddWithValue("@Gender", booking.Gender);
                cmdBooking.Parameters.AddWithValue("@Age", booking.Age);
                cmdBooking.Parameters.AddWithValue("@Address", (object?)booking.Address ?? DBNull.Value);
                cmdBooking.Parameters.AddWithValue("@PassportNo", (object?)booking.PassportNo ?? DBNull.Value);
                cmdBooking.Parameters.AddWithValue("@BoardingStationId", booking.BoardingStationId);
                cmdBooking.Parameters.AddWithValue("@DroppingStationId", booking.DroppingStationId);
                cmdBooking.Parameters.AddWithValue("@BoardingSequence", booking.BoardingSequence);
                cmdBooking.Parameters.AddWithValue("@DroppingSequence", booking.DroppingSequence);
                cmdBooking.Parameters.AddWithValue("@GrossPay", booking.GrossPay);
                cmdBooking.Parameters.AddWithValue("@Discount", booking.Discount);
                cmdBooking.Parameters.AddWithValue("@NetPay", booking.NetPay);
                cmdBooking.Parameters.AddWithValue("@PaymentMethod", booking.PaymentMethod);
                cmdBooking.Parameters.AddWithValue("@Status", booking.Status);
                cmdBooking.Parameters.AddWithValue("@IssuedAt", booking.IssuedAt);

                var bookingIdObj = await cmdBooking.ExecuteScalarAsync();
                int bookingId = Convert.ToInt32(bookingIdObj);
                booking.Id = bookingId;

                foreach (var seat in booking.Seats)
                {
                    string insertSeatSql = @"
                        INSERT INTO BookingSeats (BookingId, SeatNo, StartSequence, EndSequence)
                        VALUES (@BookingId, @SeatNo, @StartSequence, @EndSequence);
                    ";

                    using var cmdSeat = new SqlCommand(insertSeatSql, conn, tx);
                    cmdSeat.Parameters.AddWithValue("@BookingId", bookingId);
                    cmdSeat.Parameters.AddWithValue("@SeatNo", seat.SeatNo);
                    cmdSeat.Parameters.AddWithValue("@StartSequence", seat.StartSequence);
                    cmdSeat.Parameters.AddWithValue("@EndSequence", seat.EndSequence);

                    await cmdSeat.ExecuteNonQueryAsync();
                }

                tx.Commit();
                return bookingId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        public IStationRepository Stations { get; }
        public IRouteRepository Routes { get; }
        public ITripRepository Trips { get; }
        public IBookingRepository Bookings { get; }

        public UnitOfWork(IDbConnectionFactory db)
        {
            Stations = new StationAdoRepository(db);
            Routes = new RouteAdoRepository(db);
            Trips = new TripAdoRepository(db, Routes);
            Bookings = new BookingAdoRepository(db);
        }

        public void Dispose()
        {
            // Stateless ADO.NET connections disposed per operation
        }
    }
}
