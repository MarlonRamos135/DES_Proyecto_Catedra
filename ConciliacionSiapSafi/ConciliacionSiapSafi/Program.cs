using ConciliacionSiapSafi.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Sube el límite de tamaño de subida (los CSV reales pueden pesar varios MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<CodigoContratoNormalizer>();
builder.Services.AddScoped<SiapCsvReader>();
builder.Services.AddScoped<SafiCsvReader>();
builder.Services.AddScoped<SiapXlsxReader>();
builder.Services.AddScoped<SafiXlsxReader>();
builder.Services.AddScoped<ConciliacionService>();
builder.Services.AddScoped<ResultadoExcelExporter>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Conciliacion}/{action=Index}/{id?}");

app.Run();
