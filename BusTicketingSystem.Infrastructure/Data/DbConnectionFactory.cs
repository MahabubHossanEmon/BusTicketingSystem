using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BusTicketingSystem.Infrastructure.Data
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
        Task InitializeDatabaseAsync();
    }

    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Data Source=DESKTOP-5GQ647F\\SQLEXPRESS;Initial Catalog=BusTicketingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            string createTablesSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Stations')
                BEGIN
                    CREATE TABLE Stations (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(100) NOT NULL,
                        District NVARCHAR(100) NOT NULL,
                        CounterCode NVARCHAR(50) NOT NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Routes')
                BEGIN
                    CREATE TABLE Routes (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(200) NOT NULL,
                        ViaName NVARCHAR(200) NOT NULL,
                        IsActive BIT NOT NULL DEFAULT 1
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RouteStoppages')
                BEGIN
                    CREATE TABLE RouteStoppages (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        RouteId INT NOT NULL FOREIGN KEY REFERENCES Routes(Id) ON DELETE CASCADE,
                        StationId INT NOT NULL FOREIGN KEY REFERENCES Stations(Id),
                        StopSequence INT NOT NULL,
                        DistanceFromStartKm DECIMAL(18,2) NOT NULL,
                        BaseFareFromStart DECIMAL(18,2) NOT NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Trips')
                BEGIN
                    CREATE TABLE Trips (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        RouteId INT NOT NULL FOREIGN KEY REFERENCES Routes(Id),
                        CoachNo NVARCHAR(100) NOT NULL,
                        CoachType NVARCHAR(50) NOT NULL,
                        RegistrationNo NVARCHAR(100) NOT NULL,
                        DepartureTime DATETIME2 NOT NULL,
                        StartingCounter NVARCHAR(100) NOT NULL,
                        EndCounter NVARCHAR(100) NOT NULL,
                        BaseFare DECIMAL(18,2) NOT NULL,
                        TotalSeats INT NOT NULL DEFAULT 40
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bookings')
                BEGIN
                    CREATE TABLE Bookings (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        TicketNo NVARCHAR(100) NOT NULL,
                        TripId INT NOT NULL FOREIGN KEY REFERENCES Trips(Id),
                        PassengerName NVARCHAR(150) NOT NULL,
                        Mobile NVARCHAR(50) NOT NULL,
                        Gender NVARCHAR(20) NOT NULL,
                        Age INT NOT NULL,
                        Address NVARCHAR(250) NULL,
                        PassportNo NVARCHAR(100) NULL,
                        BoardingStationId INT NOT NULL,
                        DroppingStationId INT NOT NULL,
                        BoardingSequence INT NOT NULL,
                        DroppingSequence INT NOT NULL,
                        GrossPay DECIMAL(18,2) NOT NULL,
                        Discount DECIMAL(18,2) NOT NULL,
                        NetPay DECIMAL(18,2) NOT NULL,
                        PaymentMethod NVARCHAR(50) NOT NULL,
                        Status NVARCHAR(50) NOT NULL,
                        IssuedAt DATETIME2 NOT NULL
                    );
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BookingSeats')
                BEGIN
                    CREATE TABLE BookingSeats (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(Id) ON DELETE CASCADE,
                        SeatNo NVARCHAR(20) NOT NULL,
                        StartSequence INT NOT NULL,
                        EndSequence INT NOT NULL
                    );
                END;
            ";

            using var command = new SqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
