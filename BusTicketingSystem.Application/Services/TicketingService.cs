using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusTicketingSystem.Application.DTOs;
using BusTicketingSystem.Domain.Entities;
using BusTicketingSystem.Domain.Interfaces;

namespace BusTicketingSystem.Application.Services
{
    public interface ITicketingService
    {
        // Stations (Admin & User)
        Task<IEnumerable<Station>> GetAllStationsAsync();
        Task<Station?> GetStationByIdAsync(int id);
        Task<Station> CreateStationAsync(CreateStationDto dto);
        Task<bool> DeleteStationAsync(int id);
        Task<IEnumerable<Station>> GetReachableDestinationsAsync(int fromStationId);

        // Routes (Admin)
        Task<IEnumerable<AdminRouteDto>> GetAllRoutesAsync();
        Task<AdminRouteDto?> GetRouteByIdAsync(int id);
        Task<int> CreateRouteAsync(CreateRouteDto dto);
        Task<bool> DeleteRouteAsync(int id);

        // Trips (Admin & User)
        Task<IEnumerable<AdminTripDto>> GetAllTripsAdminAsync();
        Task<int> CreateTripAsync(CreateTripDto dto);
        Task<bool> DeleteTripAsync(int id);
        Task<IEnumerable<TripSearchResultDto>> SearchTripsAsync(TripSearchRequestDto request);
        Task<TripSeatLayoutDto?> GetTripSeatLayoutAsync(int tripId, int fromStationId, int toStationId, DateTime? travelDate = null);

        // Bookings / Counter
        Task<IssueTicketResponseDto> IssueTicketAsync(IssueTicketRequestDto request);
        Task<IEnumerable<AdminBookingDto>> GetAllBookingsAsync();
        Task<AdminBookingDto?> GetBookingByTicketNoAsync(string ticketNo);
    }

    public class TicketingService : ITicketingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TicketingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Stations
        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            return await _unitOfWork.Stations.GetAllAsync();
        }

        public async Task<Station?> GetStationByIdAsync(int id)
        {
            return await _unitOfWork.Stations.GetByIdAsync(id);
        }

        public async Task<Station> CreateStationAsync(CreateStationDto dto)
        {
            var station = new Station
            {
                Name = dto.Name.Trim().ToUpper(),
                District = dto.District.Trim(),
                CounterCode = dto.CounterCode.Trim().ToUpper()
            };

            await _unitOfWork.Stations.AddAsync(station);
            return station;
        }

        public async Task<bool> DeleteStationAsync(int id)
        {
            return await _unitOfWork.Stations.DeleteAsync(id);
        }

        public async Task<IEnumerable<Station>> GetReachableDestinationsAsync(int fromStationId)
        {
            var routes = await _unitOfWork.Routes.GetActiveRoutesAsync();
            var destinationSequenceMap = new Dictionary<int, int>();

            foreach (var route in routes)
            {
                var stoppages = route.Stoppages.OrderBy(s => s.StopSequence).ToList();
                var fromStop = stoppages.FirstOrDefault(s => s.StationId == fromStationId);

                if (fromStop != null)
                {
                    foreach (var stop in stoppages.Where(s => s.StopSequence > fromStop.StopSequence))
                    {
                        if (!destinationSequenceMap.ContainsKey(stop.StationId) || stop.StopSequence < destinationSequenceMap[stop.StationId])
                        {
                            destinationSequenceMap[stop.StationId] = stop.StopSequence;
                        }
                    }
                }
            }

            var allStations = await _unitOfWork.Stations.GetAllAsync();
            return allStations
                .Where(s => destinationSequenceMap.ContainsKey(s.Id))
                .OrderBy(s => destinationSequenceMap[s.Id]);
        }
        #endregion

        #region Routes
        public async Task<IEnumerable<AdminRouteDto>> GetAllRoutesAsync()
        {
            var routes = await _unitOfWork.Routes.GetAllRoutesWithStoppagesAsync();
            return routes.Select(r => new AdminRouteDto
            {
                Id = r.Id,
                Name = r.Name,
                ViaName = r.ViaName,
                IsActive = r.IsActive,
                StoppageCount = r.Stoppages.Count,
                Stoppages = r.Stoppages.OrderBy(s => s.StopSequence).Select(s => new AdminRouteStoppageDto
                {
                    Id = s.Id,
                    StationId = s.StationId,
                    StationName = s.Station?.Name ?? $"Station #{s.StationId}",
                    StopSequence = s.StopSequence,
                    DistanceFromStartKm = s.DistanceFromStartKm,
                    BaseFareFromStart = s.BaseFareFromStart
                }).ToList()
            });
        }

        public async Task<AdminRouteDto?> GetRouteByIdAsync(int id)
        {
            var r = await _unitOfWork.Routes.GetRouteWithStoppagesAsync(id);
            if (r == null) return null;

            return new AdminRouteDto
            {
                Id = r.Id,
                Name = r.Name,
                ViaName = r.ViaName,
                IsActive = r.IsActive,
                StoppageCount = r.Stoppages.Count,
                Stoppages = r.Stoppages.OrderBy(s => s.StopSequence).Select(s => new AdminRouteStoppageDto
                {
                    Id = s.Id,
                    StationId = s.StationId,
                    StationName = s.Station?.Name ?? $"Station #{s.StationId}",
                    StopSequence = s.StopSequence,
                    DistanceFromStartKm = s.DistanceFromStartKm,
                    BaseFareFromStart = s.BaseFareFromStart
                }).ToList()
            };
        }

        public async Task<int> CreateRouteAsync(CreateRouteDto dto)
        {
            var route = new Route
            {
                Name = dto.Name.Trim(),
                ViaName = dto.ViaName.Trim().ToUpper(),
                IsActive = dto.IsActive,
                Stoppages = dto.Stoppages.OrderBy(s => s.StopSequence).Select(s => new RouteStoppage
                {
                    StationId = s.StationId,
                    StopSequence = s.StopSequence,
                    DistanceFromStartKm = s.DistanceFromStartKm,
                    BaseFareFromStart = s.BaseFareFromStart
                }).ToList()
            };

            return await _unitOfWork.Routes.AddRouteWithStoppagesAsync(route);
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            return await _unitOfWork.Routes.DeleteRouteAsync(id);
        }
        #endregion

        #region Trips
        public async Task<IEnumerable<AdminTripDto>> GetAllTripsAdminAsync()
        {
            var trips = await _unitOfWork.Trips.GetAllTripsAsync();
            var result = new List<AdminTripDto>();

            foreach (var t in trips)
            {
                result.Add(new AdminTripDto
                {
                    Id = t.Id,
                    RouteId = t.RouteId,
                    RouteName = t.Route?.Name ?? string.Empty,
                    ViaName = t.Route?.ViaName ?? string.Empty,
                    CoachNo = t.CoachNo,
                    CoachType = t.CoachType,
                    RegistrationNo = t.RegistrationNo,
                    DepartureTime = t.DepartureTime,
                    StartingCounter = t.StartingCounter,
                    EndCounter = t.EndCounter,
                    BaseFare = t.BaseFare,
                    TotalSeats = t.TotalSeats
                });
            }

            return result;
        }

        public async Task<int> CreateTripAsync(CreateTripDto dto)
        {
            var trip = new Trip
            {
                RouteId = dto.RouteId,
                CoachNo = dto.CoachNo.Trim().ToUpper(),
                CoachType = dto.CoachType.Trim().ToUpper(),
                RegistrationNo = dto.RegistrationNo.Trim().ToUpper(),
                DepartureTime = dto.DepartureTime,
                StartingCounter = dto.StartingCounter.Trim().ToUpper(),
                EndCounter = dto.EndCounter.Trim().ToUpper(),
                BaseFare = dto.BaseFare,
                TotalSeats = dto.TotalSeats > 0 ? dto.TotalSeats : 40
            };

            return await _unitOfWork.Trips.AddAsync(trip);
        }

        public async Task<bool> DeleteTripAsync(int id)
        {
            return await _unitOfWork.Trips.DeleteAsync(id);
        }

        public async Task<IEnumerable<TripSearchResultDto>> SearchTripsAsync(TripSearchRequestDto request)
        {
            var trips = await _unitOfWork.Trips.SearchTripsAsync(request.FromStationId, request.ToStationId, request.DepartureDate);
            var results = new List<TripSearchResultDto>();

            foreach (var trip in trips)
            {
                if (trip.Route == null) continue;

                var fromStop = trip.Route.Stoppages.FirstOrDefault(s => s.StationId == request.FromStationId);
                var toStop = trip.Route.Stoppages.FirstOrDefault(s => s.StationId == request.ToStationId);

                if (fromStop == null || toStop == null || fromStop.StopSequence >= toStop.StopSequence)
                    continue;

                int startSeq = fromStop.StopSequence;
                int endSeq = toStop.StopSequence;

                decimal calculatedFare = 0;
                if (toStop.BaseFareFromStart > fromStop.BaseFareFromStart)
                {
                    calculatedFare = toStop.BaseFareFromStart - fromStop.BaseFareFromStart;
                }
                else
                {
                    decimal totalRouteDist = trip.Route.Stoppages.Max(s => s.DistanceFromStartKm);
                    decimal segmentDist = Math.Abs(toStop.DistanceFromStartKm - fromStop.DistanceFromStartKm);
                    calculatedFare = totalRouteDist > 0 
                        ? Math.Round((segmentDist / totalRouteDist) * trip.BaseFare, 0)
                        : trip.BaseFare;
                }

                if (calculatedFare < 50) calculatedFare = trip.BaseFare > 0 ? trip.BaseFare : 150;

                DateTime targetDate = request.DepartureDate != default ? request.DepartureDate : DateTime.Today;
                var occupiedSeats = await _unitOfWork.Bookings.GetOccupiedSeatsForTripAsync(trip.Id, startSeq, endSeq, targetDate);
                
                int soldCount = occupiedSeats.Count(s => s.Booking?.Status == "Issued");
                int bookedCount = occupiedSeats.Count(s => s.Booking?.Status == "Booked");
                int availableCount = trip.TotalSeats - soldCount - bookedCount;

                DateTime tripDeparture = targetDate.Date.Add(trip.DepartureTime.TimeOfDay);

                results.Add(new TripSearchResultDto
                {
                    TripId = trip.Id,
                    CoachNo = trip.CoachNo,
                    RouteName = trip.Route.Name,
                    ViaName = trip.Route.ViaName,
                    DepartureTime = tripDeparture.ToString("hh:mm tt dd/MM/yyyy"),
                    StartingCounter = trip.StartingCounter,
                    EndCounter = trip.EndCounter,
                    CoachType = trip.CoachType,
                    BaseFare = trip.BaseFare,
                    CalculatedSegmentFare = calculatedFare,
                    TotalSeats = trip.TotalSeats,
                    SoldCount = soldCount,
                    BookedCount = bookedCount,
                    AvailableCount = availableCount
                });
            }

            return results;
        }

        public async Task<TripSeatLayoutDto?> GetTripSeatLayoutAsync(int tripId, int fromStationId, int toStationId, DateTime? travelDate = null)
        {
            var trip = await _unitOfWork.Trips.GetTripWithDetailsAsync(tripId);
            if (trip == null || trip.Route == null) return null;

            DateTime targetDate = travelDate ?? DateTime.Today;

            var stoppages = trip.Route.Stoppages.OrderBy(s => s.StopSequence).ToList();
            var fromStop = stoppages.FirstOrDefault(s => s.StationId == fromStationId) ?? stoppages.First();
            var toStop = stoppages.FirstOrDefault(s => s.StationId == toStationId) ?? stoppages.Last();

            int startSeq = fromStop.StopSequence;
            int endSeq = toStop.StopSequence;

            decimal segFare = 0;
            if (toStop.BaseFareFromStart > fromStop.BaseFareFromStart)
            {
                segFare = toStop.BaseFareFromStart - fromStop.BaseFareFromStart;
            }
            else
            {
                decimal totalDist = stoppages.Max(s => s.DistanceFromStartKm);
                decimal segDist = Math.Abs(toStop.DistanceFromStartKm - fromStop.DistanceFromStartKm);
                segFare = totalDist > 0 ? Math.Round((segDist / totalDist) * trip.BaseFare, 0) : trip.BaseFare;
            }
            if (segFare < 50) segFare = trip.BaseFare > 0 ? trip.BaseFare : 150;

            var allOccupiedSeats = await _unitOfWork.Bookings.GetOccupiedSeatsForTripAsync(tripId, startSeq, endSeq, targetDate);

            var seatList = new List<SeatStatusDto>();
            string[] rows = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

            int soldMale = 0, soldFemale = 0, bookedMale = 0, bookedFemale = 0;

            foreach (var row in rows)
            {
                for (int col = 1; col <= 4; col++)
                {
                    string seatNo = $"{row}-{col}";
                    var occupied = allOccupiedSeats.FirstOrDefault(s => s.SeatNo == seatNo);

                    var seatStatus = new SeatStatusDto
                    {
                        SeatNo = seatNo,
                        Fare = segFare
                    };

                    if (occupied != null && occupied.Booking != null)
                    {
                        seatStatus.Status = occupied.Booking.Status == "Issued" ? "Sold" : "Booked";
                        seatStatus.PassengerName = occupied.Booking.PassengerName;
                        seatStatus.PassengerPhone = occupied.Booking.Mobile;
                        seatStatus.PassengerGender = occupied.Booking.Gender;
                        seatStatus.TicketNo = occupied.Booking.TicketNo;
                        seatStatus.OccupiedStartSequence = occupied.StartSequence;
                        seatStatus.OccupiedEndSequence = occupied.EndSequence;

                        if (occupied.Booking.Status == "Issued")
                        {
                            if (occupied.Booking.Gender == "Female") soldFemale++;
                            else soldMale++;
                        }
                        else
                        {
                            if (occupied.Booking.Gender == "Female") bookedFemale++;
                            else bookedMale++;
                        }
                    }
                    else
                    {
                        seatStatus.Status = "Available";
                    }

                    seatList.Add(seatStatus);
                }
            }

            int available = trip.TotalSeats - (soldMale + soldFemale + bookedMale + bookedFemale);

            return new TripSeatLayoutDto
            {
                TripId = trip.Id,
                CoachNo = trip.CoachNo,
                BoardingStationId = fromStop.StationId,
                DroppingStationId = toStop.StationId,
                BoardingSequence = startSeq,
                DroppingSequence = endSeq,
                SegmentFare = segFare,
                TotalSeats = trip.TotalSeats,
                SoldMaleCount = soldMale,
                SoldFemaleCount = soldFemale,
                BookedMaleCount = bookedMale,
                BookedFemaleCount = bookedFemale,
                AvailableCount = available,
                Seats = seatList,
                BoardingPoints = stoppages.Where(s => s.StopSequence < endSeq).Select(s => new StationOptionDto
                {
                    StationId = s.StationId,
                    Name = s.Station?.Name ?? string.Empty,
                    Sequence = s.StopSequence
                }).ToList(),
                DroppingPoints = stoppages.Where(s => s.StopSequence > startSeq).Select(s => new StationOptionDto
                {
                    StationId = s.StationId,
                    Name = s.Station?.Name ?? string.Empty,
                    Sequence = s.StopSequence
                }).ToList()
            };
        }
        #endregion

        #region Bookings
        public async Task<IssueTicketResponseDto> IssueTicketAsync(IssueTicketRequestDto request)
        {
            if (request.SelectedSeats == null || !request.SelectedSeats.Any())
            {
                return new IssueTicketResponseDto { Success = false, Message = "Please select at least one seat." };
            }

            var trip = await _unitOfWork.Trips.GetTripWithDetailsAsync(request.TripId);
            if (trip == null || trip.Route == null)
            {
                return new IssueTicketResponseDto { Success = false, Message = "Trip not found." };
            }

            var stoppages = trip.Route.Stoppages.OrderBy(s => s.StopSequence).ToList();
            var fromStop = stoppages.FirstOrDefault(s => s.StationId == request.BoardingStationId);
            var toStop = stoppages.FirstOrDefault(s => s.StationId == request.DroppingStationId);

            if (fromStop == null || toStop == null || fromStop.StopSequence >= toStop.StopSequence)
            {
                return new IssueTicketResponseDto { Success = false, Message = "Invalid boarding or dropping station." };
            }

            int startSeq = fromStop.StopSequence;
            int endSeq = toStop.StopSequence;

            var occupied = await _unitOfWork.Bookings.GetOccupiedSeatsForTripAsync(request.TripId, startSeq, endSeq);
            var occupiedSeatNumbers = occupied.Select(s => s.SeatNo).ToList();

            var conflictingSeats = request.SelectedSeats.Intersect(occupiedSeatNumbers).ToList();
            if (conflictingSeats.Any())
            {
                return new IssueTicketResponseDto
                {
                    Success = false,
                    Message = $"Seat(s) {string.Join(", ", conflictingSeats)} are already booked for this route segment!"
                };
            }

            decimal totalDist = stoppages.Max(s => s.DistanceFromStartKm);
            decimal segDist = Math.Abs(toStop.DistanceFromStartKm - fromStop.DistanceFromStartKm);
            decimal segFare = totalDist > 0 ? Math.Round((segDist / totalDist) * trip.BaseFare, 0) : trip.BaseFare;
            if (segFare < 150) segFare = 150;

            decimal grossPay = segFare * request.SelectedSeats.Count;
            decimal netPay = Math.Max(0, grossPay - request.Discount);

            string ticketNo = $"TKN-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var booking = new Booking
            {
                TicketNo = ticketNo,
                TripId = request.TripId,
                PassengerName = request.PassengerName,
                Mobile = request.Mobile,
                Gender = request.Gender,
                Age = request.Age,
                Address = request.Address,
                PassportNo = request.PassportNo,
                BoardingStationId = request.BoardingStationId,
                DroppingStationId = request.DroppingStationId,
                BoardingSequence = startSeq,
                DroppingSequence = endSeq,
                GrossPay = grossPay,
                Discount = request.Discount,
                NetPay = netPay,
                PaymentMethod = request.PaymentMethod,
                Status = "Issued",
                IssuedAt = DateTime.UtcNow
            };

            foreach (var seatNo in request.SelectedSeats)
            {
                booking.Seats.Add(new BookingSeat
                {
                    SeatNo = seatNo,
                    StartSequence = startSeq,
                    EndSequence = endSeq
                });
            }

            await _unitOfWork.Bookings.AddBookingAsync(booking);

            return new IssueTicketResponseDto
            {
                Success = true,
                Message = "Ticket issued successfully!",
                TicketNo = ticketNo,
                TotalAmount = netPay
            };
        }

        public async Task<IEnumerable<AdminBookingDto>> GetAllBookingsAsync()
        {
            var bookings = await _unitOfWork.Bookings.GetAllBookingsAsync();
            var stations = (await _unitOfWork.Stations.GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);

            return bookings.Select(b => new AdminBookingDto
            {
                Id = b.Id,
                TicketNo = b.TicketNo,
                TripId = b.TripId,
                CoachNo = b.Trip?.CoachNo ?? string.Empty,
                PassengerName = b.PassengerName,
                Mobile = b.Mobile,
                Gender = b.Gender,
                BoardingStation = stations.TryGetValue(b.BoardingStationId, out var bs) ? bs : $"Station #{b.BoardingStationId}",
                DroppingStation = stations.TryGetValue(b.DroppingStationId, out var ds) ? ds : $"Station #{b.DroppingStationId}",
                Seats = b.Seats.Select(s => s.SeatNo).ToList(),
                GrossPay = b.GrossPay,
                Discount = b.Discount,
                NetPay = b.NetPay,
                PaymentMethod = b.PaymentMethod,
                Status = b.Status,
                IssuedAt = b.IssuedAt
            });
        }

        public async Task<AdminBookingDto?> GetBookingByTicketNoAsync(string ticketNo)
        {
            var b = await _unitOfWork.Bookings.GetByTicketNoAsync(ticketNo);
            if (b == null) return null;

            var stations = (await _unitOfWork.Stations.GetAllAsync()).ToDictionary(s => s.Id, s => s.Name);
            var trip = await _unitOfWork.Trips.GetByIdAsync(b.TripId);

            return new AdminBookingDto
            {
                Id = b.Id,
                TicketNo = b.TicketNo,
                TripId = b.TripId,
                CoachNo = trip?.CoachNo ?? string.Empty,
                PassengerName = b.PassengerName,
                Mobile = b.Mobile,
                Gender = b.Gender,
                BoardingStation = stations.TryGetValue(b.BoardingStationId, out var bs) ? bs : $"Station #{b.BoardingStationId}",
                DroppingStation = stations.TryGetValue(b.DroppingStationId, out var ds) ? ds : $"Station #{b.DroppingStationId}",
                Seats = b.Seats.Select(s => s.SeatNo).ToList(),
                GrossPay = b.GrossPay,
                Discount = b.Discount,
                NetPay = b.NetPay,
                PaymentMethod = b.PaymentMethod,
                Status = b.Status,
                IssuedAt = b.IssuedAt
            };
        }
        #endregion
    }
}
