using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PizzaStore.Data;
using PizzaStore.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(serverUrl: "http://localhost:5341", apiKey: "n24FZWHF4gi1g5SxUp4z")
    .CreateBootstrapLogger();

try
{
    Log.Information("Web uygulaması başlatılıyor...");

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    // CORS yapılandırması
    string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
            builder =>
            {
                builder.WithOrigins("https://webmustafa.hayali.net", "https://localhost:3000")
                       .AllowAnyHeader() // Tüm başlıkları kabul et
                       .AllowAnyMethod(); // Tüm HTTP metodlarını kabul et
            });
    });

    // Yetkilendirme servisini ekle
    builder.Services.AddAuthorization();

    // Swagger yapılandırması
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pizzas API", Description = "Pizza API", Version = "v1" });
    });

    // Loglama servisini ekle
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day));

    var app = builder.Build();

    // Serilog istek loglamayı yapılandır
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "Handled {RequestPath}";
        options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    // Swagger ayarları
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pizza API V1");
    });

    // Yönlendirmeyi etkinleştir
    app.UseRouting();

    // CORS politikasını uygulama
    app.UseCors(MyAllowSpecificOrigins); // CORS politikasını burada kullan

    // Yetkilendirme
    app.UseAuthorization();

    // Ana sayfa
    app.MapGet("/", () => "Hello World!");

    // Pizza listesini alma
    app.MapGet("/pizzas", async (AppDbContext db) =>
    {
        Log.Information("Pizza listesi alınıyor.");
        var pizzas = await db.Pizzas.ToListAsync();
        if (pizzas.Count == 0)
        {
            Log.Information("Veritabanında hiç pizza kaydı bulunamadı.");
        }
        return pizzas;
    });

    // Yeni pizza ekleme
    app.MapPost("/pizzas", async (AppDbContext db, Pizza pizza) =>
    {
        Log.Information("Yeni pizza ekleniyor: {@Pizza}", pizza);
        await db.Pizzas.AddAsync(pizza);
        await db.SaveChangesAsync();
        return Results.Created($"/pizzas/{pizza.Id}", pizza);
    });

    // Pizza güncelleme
    app.MapPut("/pizzas/{id}", async (AppDbContext db, Pizza updatePizza, int id) =>
    {
        Log.Information("Pizza güncelleniyor: {@UpdatePizza}", updatePizza);
        var pizzaItem = await db.Pizzas.FindAsync(id);
        if (pizzaItem is null) return Results.NotFound();

        pizzaItem.Name = updatePizza.Name;
        pizzaItem.Description = updatePizza.Description;
        await db.SaveChangesAsync();
        return Results.NoContent();
    });

    // Pizza silme
    app.MapDelete("/pizzas/{id}", async (AppDbContext db, int id) =>
    {
        Log.Information("Pizza siliniyor: {Id}", id);
        var pizzaItem = await db.Pizzas.FindAsync(id);
        if (pizzaItem is null) return Results.NotFound();

        db.Pizzas.Remove(pizzaItem);
        await db.SaveChangesAsync();
        return Results.Ok(pizzaItem);
    });

    // Uygulamayı başlat
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılırken hata oluştu.");
}
finally
{
    Log.CloseAndFlush();
}
