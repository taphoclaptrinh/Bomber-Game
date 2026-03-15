using BomberServer.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();  // ← thêm dòng này
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapHub<GameHub>("/gamehub"); // ← thêm dòng này

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
