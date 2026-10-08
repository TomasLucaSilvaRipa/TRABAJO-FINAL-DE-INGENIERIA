using TeamBalance.BLL;
using TeamBalance.DAL;
using TeamBalance.MPP;
using TeamBalance.Services;
using Teambalance.API.Middleware;
using Teambalance.API.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
    builder.Configuration["Frontend:PublicBaseUrl"],
    builder.Configuration["Email:LogoUrl"]));
builder.Services.AddScoped<Seguridad>();

builder.Services.AddScoped<Conexion>(_ =>
    new Conexion(builder.Configuration.GetConnectionString("TeamBalanceDB")
        ?? throw new InvalidOperationException("No se configuró la cadena de conexión TeamBalanceDB.")));

builder.Services.AddScoped<MPPContratacion>();
builder.Services.AddScoped<MPPAgencia>();
builder.Services.AddScoped<MPPUsuario>();
builder.Services.AddScoped<MPPRol>();
builder.Services.AddScoped<MPPBitacora>();
builder.Services.AddScoped<MPPPlanComercial>();
builder.Services.AddScoped<MPPProyecto>();
builder.Services.AddScoped<MPPTarea>();
builder.Services.AddScoped<MPPRecursos>();
builder.Services.AddScoped<MPPSkillsDesiertos>();
builder.Services.AddScoped<MPPOpinionServicio>();
builder.Services.AddScoped<MPPConsultaPlan>();
builder.Services.AddScoped<MPPRiesgoRetraso>();
builder.Services.AddScoped<MPPRegistroHora>();
builder.Services.AddScoped<MPPBestFit>();
builder.Services.AddScoped<MPPSimulacionImpacto>();
builder.Services.AddScoped<MPPPlantillaTarea>();
builder.Services.AddScoped<MPPSuscripcion>();
builder.Services.AddScoped<MPPHelpDesk>();
builder.Services.AddScoped<MPPNovedades>();
builder.Services.AddScoped<MPPRespaldoBaseDatos>();

builder.Services.AddScoped<ContratacionBLL>();
builder.Services.AddScoped<BLLAgencia>();
builder.Services.AddScoped<BLLUsuario>();
builder.Services.AddScoped<BLLRol>();
builder.Services.AddScoped<BLLBitacora>();
builder.Services.AddScoped<BLLPlanComercial>();
builder.Services.AddScoped<BLLProyecto>();
builder.Services.AddScoped<BLLTarea>();
builder.Services.AddScoped<BLLRecursos>();
builder.Services.AddScoped<BLLSkillsDesiertos>();
builder.Services.AddScoped<BLLOpinionServicio>();
builder.Services.AddScoped<BLLConsultaPlan>();
builder.Services.AddScoped<BLLRiesgoRetraso>();
builder.Services.AddScoped<BLLReporteEjecutivo>();
builder.Services.AddScoped<BLLRegistroHora>();
builder.Services.AddScoped<BLLBestFit>();
builder.Services.AddScoped<BLLSimulacionImpacto>();
builder.Services.AddScoped<BLLPlantillaTarea>();
builder.Services.AddScoped<BLLSuscripcion>();
builder.Services.AddScoped<BLLHelpDesk>();
builder.Services.AddScoped<BLLNovedades>();
builder.Services.AddScoped<BLLRespaldoBaseDatos>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Backup").Get<BackupSettings>() ?? new BackupSettings());
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddHostedService<SubscriptionExpirationHostedService>();
builder.Services.AddHostedService<NewsPublisherHostedService>();
builder.Services.AddHostedService<BackupHostedService>();

builder.Services.AddHttpClient<PasswordSecurityWebService>( client => { client.BaseAddress = new Uri(builder.Configuration["PasswordSecurityWebService:BaseUrl"]!); });

builder.Services.AddScoped<BLLPasswordSecurity>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<SubscriptionAccessMiddleware>();

app.UseAuthorization();

app.MapControllers();


app.Run();
