using TeamBalance.BLL;
using TeamBalance.DAL;
using TeamBalance.MPP;
using TeamBalance.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<MercadoPagoService>();

builder.Services.AddScoped<Conexion>(_ =>
    new Conexion(builder.Configuration.GetConnectionString("TeamBalanceDB")
        ?? throw new InvalidOperationException("No se configuró la cadena de conexión TeamBalanceDB.")));

builder.Services.AddScoped<MPPContratacion>();
builder.Services.AddScoped<ContratacionBLL>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
