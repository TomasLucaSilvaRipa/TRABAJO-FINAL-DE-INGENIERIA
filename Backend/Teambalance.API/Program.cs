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
builder.Services.AddHttpClient("Recaptcha");
builder.Services.AddScoped<RecaptchaService>(serviceProvider => new RecaptchaService(serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Recaptcha"), builder.Configuration["Recaptcha:SecretKey"], builder.Configuration["Frontend:PublicBaseUrl"]));
builder.Services.AddSingleton(new EmailService(
    builder.Configuration["Email:Emisor"],
    builder.Configuration["Email:ClaveAplicacion"],
    builder.Configuration["Frontend:PublicBaseUrl"]));
builder.Services.AddScoped<Seguridad>();

builder.Services.AddScoped<Conexion>(_ =>
    new Conexion(builder.Configuration.GetConnectionString("TeamBalanceDB")
        ?? throw new InvalidOperationException("No se configuró la cadena de conexión TeamBalanceDB.")));

builder.Services.AddScoped<MPPContratacion>();
builder.Services.AddScoped<MPPAgencia>();
builder.Services.AddScoped<MPPUsuario>();
builder.Services.AddScoped<MPPRol>();
builder.Services.AddScoped<MPPBitacora>();

builder.Services.AddScoped<ContratacionBLL>();
builder.Services.AddScoped<BLLAgencia>();
builder.Services.AddScoped<BLLUsuario>();
builder.Services.AddScoped<BLLRol>();
builder.Services.AddScoped<BLLBitacora>();
builder.Services.AddSingleton<EncryptionService>();

builder.Services.AddHttpClient<PasswordSecurityWebService>( client => { client.BaseAddress = new Uri(builder.Configuration["PasswordSecurityWebService:BaseUrl"]!); });

builder.Services.AddScoped<BLLPasswordSecurity>();

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
