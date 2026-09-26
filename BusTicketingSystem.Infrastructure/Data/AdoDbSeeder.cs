using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace BusTicketingSystem.Infrastructure.Data
{
    public static class AdoDbSeeder
    {
        public static async Task SeedIfEmptyAsync(IDbConnectionFactory db)
        {
            using var conn = db.CreateConnection();
            await conn.OpenAsync();

            // Check if route stoppages exist with full coverage
            string checkSql = "SELECT COUNT(*) FROM RouteStoppages;";
            using var checkCmd = new SqlCommand(checkSql, conn);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count >= 15) return; // Database already contains full routes with stoppages

            string seedSql = @"
                -- Clear existing incomplete seed if any
                DELETE FROM BookingSeats;
                DELETE FROM Bookings;
                DELETE FROM Trips;
                DELETE FROM RouteStoppages;
                DELETE FROM Routes;
                DELETE FROM Stations;
                -- 1. Initial Stations
                INSERT INTO Stations (Name, District, CounterCode) VALUES 
                (N'VERAMARA', N'Kushtia', N'VRM'),
                (N'KUSHTIA', N'Kushtia', N'KST'),
                (N'SOILKUPA', N'Jhenaidah', N'SLK'),
                (N'KHOKSA', N'Kushtia', N'KHK'),
                (N'RAJBARI', N'Rajbari', N'RJB'),
                (N'MANIKGONJ', N'Manikgonj', N'MNK'),
                (N'GABTOLI (DHAKA)', N'Dhaka', N'GBT'),
                (N'SAYDABAD (DHAKA)', N'Dhaka', N'SYD'),
                (N'FARIDPUR', N'Faridpur', N'FDP'),
                (N'MAGURA', N'Magura', N'MGR');

                DECLARE @stVeramara INT = (SELECT Id FROM Stations WHERE CounterCode = 'VRM');
                DECLARE @stKushtia INT = (SELECT Id FROM Stations WHERE CounterCode = 'KST');
                DECLARE @stSoilkupa INT = (SELECT Id FROM Stations WHERE CounterCode = 'SLK');
                DECLARE @stKhoksa INT = (SELECT Id FROM Stations WHERE CounterCode = 'KHK');
                DECLARE @stRajbari INT = (SELECT Id FROM Stations WHERE CounterCode = 'RJB');
                DECLARE @stManikgonj INT = (SELECT Id FROM Stations WHERE CounterCode = 'MNK');
                DECLARE @stGabtoli INT = (SELECT Id FROM Stations WHERE CounterCode = 'GBT');
                DECLARE @stSaydabad INT = (SELECT Id FROM Stations WHERE CounterCode = 'SYD');
                DECLARE @stFaridpur INT = (SELECT Id FROM Stations WHERE CounterCode = 'FDP');
                DECLARE @stMagura INT = (SELECT Id FROM Stations WHERE CounterCode = 'MGR');

                -- 2. Route: Veramara -> Dhaka via Aricha
                INSERT INTO Routes (Name, ViaName, IsActive) VALUES (N'Veramara - Kushtia - Dhaka via Aricha', N'VIA ARICHA', 1);
                DECLARE @r1 INT = SCOPE_IDENTITY();
                INSERT INTO RouteStoppages (RouteId, StationId, StopSequence, DistanceFromStartKm, BaseFareFromStart) VALUES
                (@r1, @stVeramara, 1, 0, 0),
                (@r1, @stKushtia, 2, 20, 70),
                (@r1, @stSoilkupa, 3, 40, 140),
                (@r1, @stKhoksa, 4, 65, 220),
                (@r1, @stRajbari, 5, 105, 370),
                (@r1, @stManikgonj, 6, 185, 570),
                (@r1, @stGabtoli, 7, 250, 750);

                -- 3. Route: Veramara -> Dhaka via Padma Bridge
                INSERT INTO Routes (Name, ViaName, IsActive) VALUES (N'Veramara - Kushtia - Dhaka via Padma Bridge', N'VIA PADMA BRIDGE', 1);
                DECLARE @r2 INT = SCOPE_IDENTITY();
                INSERT INTO RouteStoppages (RouteId, StationId, StopSequence, DistanceFromStartKm, BaseFareFromStart) VALUES
                (@r2, @stVeramara, 1, 0, 0),
                (@r2, @stKushtia, 2, 20, 70),
                (@r2, @stSoilkupa, 3, 40, 140),
                (@r2, @stMagura, 4, 70, 240),
                (@r2, @stFaridpur, 5, 120, 400),
                (@r2, @stGabtoli, 6, 260, 750);

                -- 4. Return Route: Dhaka (Gabtoli) -> Kushtia via Aricha
                INSERT INTO Routes (Name, ViaName, IsActive) VALUES (N'Dhaka (Gabtoli) - Kushtia via Aricha', N'VIA ARICHA', 1);
                DECLARE @r3 INT = SCOPE_IDENTITY();
                INSERT INTO RouteStoppages (RouteId, StationId, StopSequence, DistanceFromStartKm, BaseFareFromStart) VALUES
                (@r3, @stGabtoli, 1, 0, 0),
                (@r3, @stManikgonj, 2, 65, 200),
                (@r3, @stRajbari, 3, 145, 400),
                (@r3, @stKhoksa, 4, 185, 550),
                (@r3, @stSoilkupa, 5, 210, 630),
                (@r3, @stKushtia, 6, 230, 680);

                -- 5. Return Route: Dhaka (Gabtoli) -> Kushtia via Padma Bridge
                INSERT INTO Routes (Name, ViaName, IsActive) VALUES (N'Dhaka (Gabtoli) - Kushtia via Padma Bridge', N'VIA PADMA BRIDGE', 1);
                DECLARE @r4 INT = SCOPE_IDENTITY();
                INSERT INTO RouteStoppages (RouteId, StationId, StopSequence, DistanceFromStartKm, BaseFareFromStart) VALUES
                (@r4, @stGabtoli, 1, 0, 0),
                (@r4, @stFaridpur, 2, 140, 400),
                (@r4, @stMagura, 3, 190, 550),
                (@r4, @stSoilkupa, 4, 220, 630),
                (@r4, @stKushtia, 5, 240, 680);

                -- 6. Initial Trips
                DECLARE @today DATETIME2 = CAST(GETDATE() AS DATE);

                INSERT INTO Trips (RouteId, CoachNo, CoachType, RegistrationNo, DepartureTime, StartingCounter, EndCounter, BaseFare, TotalSeats) VALUES
                (@r2, N'21 SOIL-DHK (PADMA)', N'NON AC', N'DHAKA METRO-BA-14-9821', DATEADD(HOUR, 7, @today), N'VERAMARA', N'GABTOLI', 750, 40),
                (@r1, N'71 SOIL-PANT-DHK (ARICHA)', N'NON AC', N'DHAKA METRO-BA-14-7171', DATEADD(MINUTE, 450, @today), N'VERAMARA', N'GABTOLI', 650, 40),
                (@r1, N'22 SOIL-DHK (ARICHA)', N'NON AC', N'DHAKA METRO-BA-14-2222', DATEADD(HOUR, 9, @today), N'VERAMARA', N'GABTOLI', 750, 40),
                (@r1, N'24 SOIL-GAZI-DHK (ARICHA)', N'NON AC', N'DHAKA METRO-BA-14-2424', DATEADD(MINUTE, 1215, @today), N'VERAMARA', N'SAYDABAD COUNTER', 750, 40),
                (@r2, N'25 SOIL-DHK (PADMA)', N'NON AC', N'DHAKA METRO-BA-14-2525', DATEADD(MINUTE, 1290, @today), N'VERAMARA', N'GABTOLI', 750, 40),
                (@r4, N'31 DHK-SOIL (PADMA)', N'NON AC', N'DHAKA METRO-BA-14-3131', DATEADD(HOUR, 8, @today), N'GABTOLI', N'KUSHTIA', 750, 40),
                (@r3, N'32 DHK-SOIL (ARICHA)', N'NON AC', N'DHAKA METRO-BA-14-3232', DATEADD(MINUTE, 630, @today), N'GABTOLI', N'KUSHTIA', 650, 40),
                (@r4, N'33 DHK-SOIL (PADMA)', N'NON AC', N'DHAKA METRO-BA-14-3333', DATEADD(MINUTE, 1230, @today), N'GABTOLI', N'KUSHTIA', 750, 40);

                -- Initial Bookings
                DECLARE @trip1Id INT = (SELECT TOP 1 Id FROM Trips WHERE CoachNo = '21 SOIL-DHK (PADMA)');

                INSERT INTO Bookings (TicketNo, TripId, PassengerName, Mobile, Gender, Age, Address, PassportNo, BoardingStationId, DroppingStationId, BoardingSequence, DroppingSequence, GrossPay, Discount, NetPay, PaymentMethod, Status, IssuedAt)
                VALUES ('TKN-SEED-M1', @trip1Id, 'MD. RAFIQUL ISLAM', '01712345678', 'Male', 32, 'Veramara, Kushtia', '', @stVeramara, @stGabtoli, 1, 6, 750, 0, 750, 'Cash', 'Issued', GETUTCDATE());
                DECLARE @bk1 INT = SCOPE_IDENTITY();
                INSERT INTO BookingSeats (BookingId, SeatNo, StartSequence, EndSequence) VALUES 
                (@bk1, 'A-1', 1, 6), (@bk1, 'B-1', 1, 6), (@bk1, 'B-2', 1, 6), (@bk1, 'C-1', 1, 6), (@bk1, 'C-2', 1, 6), (@bk1, 'D-1', 1, 6), (@bk1, 'D-4', 1, 6);

                INSERT INTO Bookings (TicketNo, TripId, PassengerName, Mobile, Gender, Age, Address, PassportNo, BoardingStationId, DroppingStationId, BoardingSequence, DroppingSequence, GrossPay, Discount, NetPay, PaymentMethod, Status, IssuedAt)
                VALUES ('TKN-SEED-F1', @trip1Id, 'NUSRAT JAHAN', '01819876543', 'Female', 26, 'Kushtia Sadar', '', @stVeramara, @stGabtoli, 1, 6, 750, 0, 750, 'Cash', 'Issued', GETUTCDATE());
                DECLARE @bk2 INT = SCOPE_IDENTITY();
                INSERT INTO BookingSeats (BookingId, SeatNo, StartSequence, EndSequence) VALUES 
                (@bk2, 'A-3', 1, 6), (@bk2, 'A-4', 1, 6), (@bk2, 'C-4', 1, 6);

                INSERT INTO Bookings (TicketNo, TripId, PassengerName, Mobile, Gender, Age, Address, PassportNo, BoardingStationId, DroppingStationId, BoardingSequence, DroppingSequence, GrossPay, Discount, NetPay, PaymentMethod, Status, IssuedAt)
                VALUES ('TKN-HOLD-F1', @trip1Id, 'FARHANA AKTER', '01915554433', 'Female', 24, 'Dhaka Dhanmondi', '', @stVeramara, @stGabtoli, 1, 6, 750, 0, 750, 'Cash', 'Booked', GETUTCDATE());
                DECLARE @bk3 INT = SCOPE_IDENTITY();
                INSERT INTO BookingSeats (BookingId, SeatNo, StartSequence, EndSequence) VALUES 
                (@bk3, 'A-2', 1, 6), (@bk3, 'B-4', 1, 6), (@bk3, 'C-3', 1, 6), (@bk3, 'E-1', 1, 6);
            ";

            using var cmd = new SqlCommand(seedSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
