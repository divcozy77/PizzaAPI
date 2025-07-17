using System.Globalization;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<PizzaDbContext>();
builder.Services.AddEndpointsApiExplorer(); // enables minimal API discovery
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pizza API",
        Version = "v1",
        Description = "Minimal API for Pizza Orders"
    });
});

// Register SPA
builder.Services.AddSpaStaticFiles(options =>
{
    options.RootPath = "ClientApp/dist/browser";
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

// Enable HTTPS and static files
app.UseHttpsRedirection();
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseSpaStaticFiles();
}


// Ensure DB exists and import CSV on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();
    db.Database.EnsureCreated();
    DataImporter.ImportData(db);
}

// API Endpoints
app.MapGet("/api/pizzas", (
    [FromQuery] string? type,
    [FromQuery] string? size,
    PizzaDbContext db) =>
{
    var query = db.Pizzas.AsQueryable();
    if (!string.IsNullOrEmpty(type)) query = query.Where(p => p.PizzaTypeId == type);
    if (!string.IsNullOrEmpty(size)) query = query.Where(p => p.Size == size);
    return query.ToList();
});

app.MapGet("/api/pizza-types", (PizzaDbContext db) => db.PizzaTypes.ToList());

app.MapGet("/api/orders", (PizzaDbContext db, DateTime? from, DateTime? to) =>
{
    var query = db.Orders.AsQueryable();
    if (from.HasValue)
        query = query.Where(o => o.Date >= from);
    if (to.HasValue)
        query = query.Where(o => o.Date <= to);
    return query.ToList();
});

app.MapGet("/api/order-details", (PizzaDbContext db) => db.OrderDetails.ToList());

app.MapGet("/api/export/pizzas/csv", (PizzaDbContext db) =>
{
    var records = db.Pizzas.ToList();
    using var memoryStream = new MemoryStream();
    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
    csv.WriteRecords(records);
    writer.Flush();
    return Results.File(memoryStream.ToArray(), "text/csv", "pizzas.csv");
});

app.MapGet("/api/export/orders/json", (PizzaDbContext db) =>
{
    var records = db.Orders.ToList();
    return Results.Json(records);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pizza API v1");
        c.RoutePrefix = "swagger"; 
    });
}


// Static + SPA handling
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    then => then.UseSpa(spa =>
    {
        const int port = 4200;

        spa.Options.SourcePath = "ClientApp";
        spa.Options.DevServerPort = port;
        spa.Options.PackageManagerCommand = "npm";

        if (app.Environment.IsDevelopment())
        {
            spa.UseAngularCliServer("asp");
            spa.UseProxyToSpaDevelopmentServer($"http://localhost:{port}");
        }
    }));

app.Run();

// Models
public class PizzaType
{
    [CsvHelper.Configuration.Attributes.Name("pizza_type_id")]
    public required string PizzaTypeId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("name")]
    public required string Name { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("category")]
    public required string Category { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("ingredients")]
    public required string Ingredients { get; set; }
}

public class Pizza
{
    [CsvHelper.Configuration.Attributes.Name("pizza_id")]
    public required string PizzaId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("pizza_type_id")]
    public required string PizzaTypeId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("size")]
    public required string Size { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("price")]
    public decimal Price { get; set; }
}

public class Order
{
    [CsvHelper.Configuration.Attributes.Name("order_id")]
    public int OrderId { get; set; }    
    public DateTime Date { get; set; }
}

public class OrderDetail
{
    [CsvHelper.Configuration.Attributes.Name("order_details_id")]
    public int OrderDetailsId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("order_id")]
    public int OrderId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("pizza_id")]
    public required string PizzaId { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("quantity")]
    public int Quantity { get; set; }
}

// DbContext
public class PizzaDbContext : DbContext
{
    public DbSet<PizzaType> PizzaTypes => Set<PizzaType>();
    public DbSet<Pizza> Pizzas => Set<Pizza>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=pizza.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PizzaType>().HasKey(od => od.PizzaTypeId);
        modelBuilder.Entity<Pizza>().HasKey(od => od.PizzaId);
        modelBuilder.Entity<Order>().HasKey(od => od.OrderId);
        modelBuilder.Entity<OrderDetail>().HasKey(od => od.OrderDetailsId);
    }
}

// CSV Importer
public static class DataImporter
{
    public static void ImportData(PizzaDbContext db)
    {
        if (db.Pizzas.Any()) return; // Skip if already imported

        using var pizzaTypesReader = new StreamReader("pizzas/pizza_types.csv");
        using var pizzaReader = new StreamReader("pizzas/pizzas.csv");
        using var orderReader = new StreamReader("pizzas/orders.csv");
        using var orderDetailsReader = new StreamReader("pizzas/order_details.csv");

        using var csvPizzaTypes = new CsvReader(pizzaTypesReader, CultureInfo.InvariantCulture);
        var pizzaTypes = csvPizzaTypes.GetRecords<PizzaType>().ToList();

        using var csvPizzas = new CsvReader(pizzaReader, CultureInfo.InvariantCulture);
        var pizzas = csvPizzas.GetRecords<Pizza>().ToList();

        using var csvOrders = new CsvReader(orderReader, CultureInfo.InvariantCulture);
        var rawOrders = csvOrders.GetRecords<OrderRaw>().ToList();
        var orders = rawOrders.Select(o => new Order
        {
            OrderId = o.OrderId,
            Date = DateTime.Parse($"{o.Date} {o.Time}")
        }).ToList();

        using var csvOrderDetails = new CsvReader(orderDetailsReader, CultureInfo.InvariantCulture);
        var orderDetails = csvOrderDetails.GetRecords<OrderDetail>().ToList();

        db.PizzaTypes.AddRange(pizzaTypes);
        db.Pizzas.AddRange(pizzas);
        db.Orders.AddRange(orders);
        db.OrderDetails.AddRange(orderDetails);
        db.SaveChanges();
    }

    private class OrderRaw
    {
        [CsvHelper.Configuration.Attributes.Name("order_id")]
        public int OrderId { get; set; }
        [CsvHelper.Configuration.Attributes.Name("date")]
        public required string Date { get; set; }
        [CsvHelper.Configuration.Attributes.Name("time")]
        public required string Time { get; set; }
    }
}