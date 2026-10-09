using TicketshineSupportCopilot.Api.Models;
using TicketshineSupportCopilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(sp => AiOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton(sp => MatrixOneOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddHttpClient<IAiClient, OpenAiClient>();
builder.Services.AddScoped<IMatrixOneRepository, MatrixOneRepository>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("LocalFrontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/ingestion/run", async (IngestionService ingestion, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await ingestion.RunAsync(cancellationToken));
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "AI setup required");
    }
    catch (MySqlConnector.MySqlException ex)
    {
        return Results.Problem(
            $"MatrixOne is unavailable. Start Docker with `docker compose up -d`. Details: {ex.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "MatrixOne unavailable");
    }
});

app.MapGet("/api/documents", async (IMatrixOneRepository repository, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await repository.ListDocumentsAsync(cancellationToken));
    }
    catch (MySqlConnector.MySqlException ex)
    {
        return Results.Problem(
            $"MatrixOne is unavailable. Start Docker with `docker compose up -d`. Details: {ex.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "MatrixOne unavailable");
    }
});

app.MapPost("/api/chat", async (ChatRequest request, ChatService chat, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new { error = "Question is required." });
    }

    try
    {
        return Results.Ok(await chat.AnswerAsync(request, cancellationToken));
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "AI setup required");
    }
    catch (MySqlConnector.MySqlException ex)
    {
        return Results.Problem(
            $"MatrixOne is unavailable. Start Docker with `docker compose up -d`. Details: {ex.Message}",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "MatrixOne unavailable");
    }
});

app.Run();

public partial class Program;
