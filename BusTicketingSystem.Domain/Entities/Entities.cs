using System;
using System.Collections.Generic;

namespace BusTicketingSystem.Domain.Entities
{
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string CounterCode { get; set; } = string.Empty;
    }

    public class Route
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., Kushtia - Dhaka
        public string ViaName { get; set; } = string.Empty; // e.g., Via Aricha / Via Padma Bridge
        public bool IsActive { get; set; } = true;

        public ICollection<RouteStoppage> Stoppages { get; set; } = new List<RouteStoppage>();
    }

    public class RouteStoppage
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int StationId { get; set; }
        public int StopSequence { get; set; } // 1, 2, 3, 4, 5...
        public decimal DistanceFromStartKm { get; set; }
        public decimal BaseFareFromStart { get; set; }

        public Station? Station { get; set; }
    }

    public class Trip
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public string CoachNo { get; set; } = string.Empty; // e.g., 21 SOIL-DHK (PADMA)
        public string CoachType { get; set; } = "NON AC"; // NON AC, E-CLASS, BUSINESS
        public string RegistrationNo { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string StartingCounter { get; set; } = string.Empty;
        public string EndCounter { get; set; } = string.Empty;
        public decimal BaseFare { get; set; }
        public int TotalSeats { get; set; } = 40;

        public Route? Route { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public class Booking
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = string.Empty;
        public int TripId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Gender { get; set; } = "Male"; // Male, Female
        public int Age { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PassportNo { get; set; } = string.Empty;
        
        public int BoardingStationId { get; set; }
        public int DroppingStationId { get; set; }
        public int BoardingSequence { get; set; }
        public int DroppingSequence { get; set; }

        public decimal GrossPay { get; set; }
        public decimal Discount { get; set; }
        public decimal NetPay { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, bKash
        public string Status { get; set; } = "Issued"; // Issue, Booked, Cancelled

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public Trip? Trip { get; set; }
        public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
    }

    public class BookingSeat
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string SeatNo { get; set; } = string.Empty; // A-1, B-2...
        public int StartSequence { get; set; }
        public int EndSequence { get; set; }

        public Booking? Booking { get; set; }
    }

    public class SeatStatusDto
    {
        public string SeatNo { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Sold, Booked, Blocked
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public string PassengerGender { get; set; } = string.Empty;
        public string TicketNo { get; set; } = string.Empty;
        public decimal Fare { get; set; }
        public int? OccupiedStartSequence { get; set; }
        public int? OccupiedEndSequence { get; set; }
    }
}
