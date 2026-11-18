using System.Data.Common;
using Cinema.Contracts;
using Npgsql;

namespace Cinema.Api.Services;

/// <summary>Direct ADO.NET example: DbConnection, DbCommand, DbParameter, DbDataReader and DbTransaction.</summary>
public sealed class AdoNetTicketRepository(IConfiguration configuration)
{
    public async Task<TicketDto> SellTicketAsync(TicketCreateDto request, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения к PostgreSQL.");

        await using DbConnection connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            decimal price;
            int capacity;
            bool seatTaken;

            await using (DbCommand checkCommand = connection.CreateCommand())
            {
                checkCommand.Transaction = transaction;
                checkCommand.CommandText = """
                    SELECT s.price, h.capacity,
                           EXISTS (SELECT 1 FROM tickets t
                                   WHERE t.screening_id = s.id AND t.seat_number = @seat_number)
                    FROM screenings s
                    JOIN halls h ON h.id = s.hall_id
                    WHERE s.id = @screening_id
                    """;
                AddParameter(checkCommand, "screening_id", request.ScreeningId);
                AddParameter(checkCommand, "seat_number", request.SeatNumber);

                await using DbDataReader reader = await checkCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("Сеанс не найден.");

                price = reader.GetDecimal(0);
                capacity = reader.GetInt32(1);
                seatTaken = reader.GetBoolean(2);
            }

            if (request.SeatNumber < 1 || request.SeatNumber > capacity)
                throw new InvalidOperationException($"Номер места должен быть от 1 до {capacity}.");
            if (seatTaken)
                throw new InvalidOperationException("Это место уже продано.");

            var soldAt = DateTime.UtcNow;
            int ticketId;
            await using (DbCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO tickets (screening_id, seat_number, customer_name, price, sold_at_utc)
                    VALUES (@screening_id, @seat_number, @customer_name, @price, @sold_at_utc)
                    RETURNING id
                    """;
                AddParameter(insertCommand, "screening_id", request.ScreeningId);
                AddParameter(insertCommand, "seat_number", request.SeatNumber);
                AddParameter(insertCommand, "customer_name", request.CustomerName.Trim());
                AddParameter(insertCommand, "price", price);
                AddParameter(insertCommand, "sold_at_utc", soldAt);
                ticketId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync(cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
            return new TicketDto(ticketId, request.ScreeningId, request.SeatNumber, request.CustomerName.Trim(), price, soldAt);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
