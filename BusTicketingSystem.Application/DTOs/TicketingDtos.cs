using System;
using System.Collections.Generic;
using BusTicketingSystem.Domain.Entities;

namespace BusTicketingSystem.Application.DTOs
{
    public class TripSearchRequestDto
    {
        public int FromStationId { get; set; }
        public int ToStationId { get; set; }
        public DateTime DepartureDate { get; set; }
    }

    public class TripSearchResultDto
    {
        public int TripId { get; set; }
        public string CoachNo { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string ViaName { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
        public string StartingCounter { get; set; } = string.Empty;
        public string EndCounter { get; set; } = string.Empty;
        public string CoachType { get; set; } = "NON AC";
        public decimal BaseFare { get; set; }
        public decimal CalculatedSegmentFare { get; set; }
        public int TotalSeats { get; set; }
        public int SoldCount { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCount { get; set; }
    }

    public class TripSeatLayoutDto
    {
        public int TripId { get; set; }
        public string CoachNo { get; set; } = string.Empty;
        public int BoardingStationId { get; set; }
        public int DroppingStationId { get; set; }
        public int BoardingSequence { get; set; }
        public int DroppingSequence { get; set; }
        public decimal SegmentFare { get; set; }
        public int TotalSeats { get; set; } = 40;
        public int SoldMaleCount { get; set; }
        public int SoldFemaleCount { get; set; }
        public int BookedMaleCount { get; set; }
        public int BookedFemaleCount { get; set; }
        public int AvailableCount { get; set; }
        public List<SeatStatusDto> Seats { get; set; } = new List<SeatStatusDto>();
        public List<StationOptionDto> BoardingPoints { get; set; } = new List<StationOptionDto>();
        public List<StationOptionDto> DroppingPoints { get; set; } = new List<StationOptionDto>();
    }

    public class StationOptionDto
    {
        public int StationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sequence { get; set; }
    }

    public class IssueTicketRequestDto
    {
        public int TripId { get; set; }
        public List<string> SelectedSeats { get; set; } = new List<string>();
        public string PassengerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male";
        public int Age { get; set; } = 25;
        public string Address { get; set; } = string.Empty;
        public string PassportNo { get; set; } = string.Empty;
        public int BoardingStationId { get; set; }
        public int DroppingStationId { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class IssueTicketResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TicketNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class CreateStationDto
    {
        public string Name { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string CounterCode { get; set; } = string.Empty;
    }

    public class CreateRouteDto
    {
        public string Name { get; set; } = string.Empty;
        public string ViaName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<CreateRouteStoppageDto> Stoppages { get; set; } = new List<CreateRouteStoppageDto>();
    }

    public class CreateRouteStoppageDto
    {
        public int StationId { get; set; }
        public int StopSequence { get; set; }
        public decimal DistanceFromStartKm { get; set; }
        public decimal BaseFareFromStart { get; set; }
    }

    public class CreateTripDto
    {
        public int RouteId { get; set; }
        public string CoachNo { get; set; } = string.Empty;
        public string CoachType { get; set; } = "NON AC";
        public string RegistrationNo { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string StartingCounter { get; set; } = string.Empty;
        public string EndCounter { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public int TotalSeats { get; set; } = 40;
    }

    public class AdminRouteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ViaName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int StoppageCount { get; set; }
        public List<AdminRouteStoppageDto> Stoppages { get; set; } = new List<AdminRouteStoppageDto>();
    }

    public class AdminRouteStoppageDto
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int StopSequence { get; set; }
        public decimal DistanceFromStartKm { get; set; }
        public decimal BaseFareFromStart { get; set; }
    }

    public class AdminTripDto
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string ViaName { get; set; } = string.Empty;
        public string CoachNo { get; set; } = string.Empty;
        public string CoachType { get; set; } = "NON AC";
        public string RegistrationNo { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string StartingCounter { get; set; } = string.Empty;
        public string EndCounter { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeatsCount { get; set; }
    }

    public class AdminBookingDto
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string CoachNo { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string BoardingStation { get; set; } = string.Empty;
        public string DroppingStation { get; set; } = string.Empty;
        public List<string> Seats { get; set; } = new List<string>();
        public decimal GrossPay { get; set; }
        public decimal Discount { get; set; }
        public decimal NetPay { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
    }
}
