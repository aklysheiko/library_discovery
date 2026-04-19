using LibraryDiscovery.Application.Interfaces;
using LibraryDiscovery.Application.Services;
using LibraryDiscovery.Infrastructure;
using LibraryDiscovery.Infrastructure.Llm;
using LibraryDiscovery.Infrastructure.Normalization;
using LibraryDiscovery.Infrastructure.OpenLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS — origins read from config (AllowedOrigins array).
// In development the default is localhost:5173; in production the CI/CD
// pipeline sets AllowedOrigins__0 to the Static Web App URL.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register infrastructure services
builder.Services.AddHttpClient<IOpenLibrarySearchService, OpenLibrarySearchService>();
builder.Services.AddScoped<IStringNormalizationService, StringNormalizationService>();
builder.Services.AddScoped<ICandidateEnrichmentService, CandidateEnrichmentService>();
builder.Services.AddScoped<IExplanationBuilder, ExplanationBuilder>();

// Register query parsing services
builder.Services.AddScoped<IQueryParsingFallback, FallbackQueryParser>();
var geminiKey = builder.Configuration["GEMINI_API_KEY"];
if (!string.IsNullOrEmpty(geminiKey))
{
    // Use Gemini when API key is available
    builder.Services.AddHttpClient<IQueryParsingService, GeminiQueryParsingService>();
}
else
{
    // Default to non-LLM parser
    builder.Services.AddScoped<IQueryParsingService, QueryParsingService>();
}

// Register application services
builder.Services.AddScoped<IBookMatcher, BookMatcherService>();
builder.Services.AddScoped<IBookMatchService, BookMatchService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactFrontend");
app.MapControllers();

app.Run();
