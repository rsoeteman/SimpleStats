using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimpleStats.Data;

namespace SimpleStats.Tests.TestSupport;

/// <summary>
/// Creates a <see cref="SimpleStatsContext"/> backed by an open, in-memory SQLite connection with
/// the schema already created. The connection is kept open by the caller for the lifetime of the
/// database (SQLite's <c>:memory:</c> database is destroyed the moment its one connection closes),
/// so this is a relational engine rather than EF's InMemory provider — required for tests that
/// exercise <c>IRelationalDatabaseCreator</c> or the unique index on (NodeKey, Date).
/// </summary>
internal static class SqliteInMemoryContextFactory
{
    public static (SqliteConnection Connection, SimpleStatsContext Context) CreateWithSchema()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        DbContextOptions<SimpleStatsContext> options = new DbContextOptionsBuilder<SimpleStatsContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SimpleStatsContext(options);
        context.Database.EnsureCreated();

        return (connection, context);
    }
}
