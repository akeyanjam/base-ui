using BofA.MBSS.ChnageLog.Services;
using BofA.MBSS.ChnageLog.Services.Bitbucket;
using BofA.MBSS.ChnageLog.Services.Jira;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SIT Changelog Builder API", Version = "v1" });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure HttpClients with authentication
builder.Services.AddHttpClient<IBitbucketService, BitbucketService>("Bitbucket", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Bitbucket:BaseUrl"] ?? throw new InvalidOperationException("Bitbucket:BaseUrl is required");
    var accessToken = configuration["Bitbucket:AccessToken"] ?? throw new InvalidOperationException("Bitbucket:AccessToken is required");
    var timeoutSeconds = configuration.GetValue<int>("Bitbucket:TimeoutSeconds", 30);

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    
    client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
});

builder.Services.AddHttpClient<IJiraService, JiraService>("Jira", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Jira:BaseUrl"] ?? throw new InvalidOperationException("Jira:BaseUrl is required");
    var accessToken = configuration["Jira:AccessToken"] ?? throw new InvalidOperationException("Jira:AccessToken is required");
    var timeoutSeconds = configuration.GetValue<int>("Jira:TimeoutSeconds", 30);

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new("application/json"));
    
    client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
});

// Register application services
builder.Services.AddScoped<IRequestScopedCache, RequestScopedCache>();
builder.Services.AddScoped<IChangelogOrchestrator, ChangelogOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIT Changelog Builder API v1");
        c.RoutePrefix = string.Empty; // Serve swagger at root
    });
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
