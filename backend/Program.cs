using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Crée la table si elle n'existe pas
await EnsureTableAsync(builder.Configuration.GetConnectionString("AzureSQL")!);

app.MapPost("/api/ideas", async (VideoIdeaRequest request, IConfiguration config) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Idea))
        return Results.BadRequest("Le titre et l'idée sont requis.");

    var connectionString = config.GetConnectionString("AzureSQL")!;

    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand(
        "INSERT INTO VideoIdeas (Title, Idea, SubmittedAt) VALUES (@title, @idea, @submittedAt)",
        conn);
    cmd.Parameters.AddWithValue("@title", request.Title.Trim());
    cmd.Parameters.AddWithValue("@idea", request.Idea.Trim());
    cmd.Parameters.AddWithValue("@submittedAt", DateTime.UtcNow);

    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new { message = "Idée enregistrée avec succès." });
});

app.Run();

static async Task EnsureTableAsync(string connectionString)
{
    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand("""
        IF NOT EXISTS (
            SELECT * FROM sysobjects WHERE name='VideoIdeas' AND xtype='U'
        )
        CREATE TABLE VideoIdeas (
            Id          INT IDENTITY(1,1) PRIMARY KEY,
            Title       NVARCHAR(120)     NOT NULL,
            Idea        NVARCHAR(MAX)     NOT NULL,
            SubmittedAt DATETIME2         NOT NULL
        )
        """, conn);

    await cmd.ExecuteNonQueryAsync();
}

record VideoIdeaRequest(string Title, string Idea);
