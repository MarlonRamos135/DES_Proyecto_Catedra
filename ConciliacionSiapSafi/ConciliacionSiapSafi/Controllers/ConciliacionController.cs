using Microsoft.AspNetCore.Mvc;
using ConciliacionSiapSafi.Services;
using System.Text.Json;

namespace ConciliacionSiapSafi.Controllers;

public class ConciliacionController : Controller
{
    private readonly SiapCsvReader _siapReader;
    private readonly SafiCsvReader _safiReader;
    private readonly SiapXlsxReader _siapXlsxReader;
    private readonly SafiXlsxReader _safiXlsxReader;
    private readonly ConciliacionService _conciliacionService;
    private readonly ResultadoExcelExporter _resultadoExcelExporter;

    public ConciliacionController(
        SiapCsvReader siapReader,
        SafiCsvReader safiReader,
        SiapXlsxReader siapXlsxReader,
        SafiXlsxReader safiXlsxReader,
        ConciliacionService conciliacionService,
        ResultadoExcelExporter resultadoExcelExporter)
    {
        _siapReader = siapReader;
        _safiReader = safiReader;
        _siapXlsxReader = siapXlsxReader;
        _safiXlsxReader = safiXlsxReader;
        _conciliacionService = conciliacionService;
        _resultadoExcelExporter = resultadoExcelExporter;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public IActionResult Comparar(IFormFile archivoSiap, IFormFile archivoSafi)
    {
        if (archivoSiap == null || archivoSiap.Length == 0 ||
            archivoSafi == null || archivoSafi.Length == 0)
        {
            ModelState.AddModelError("", "Debe subir ambos archivos (SIAP y SAFI), en formato .csv o .xlsx.");
            return View("Index");
        }

        List<Models.ContratoSiap> siap;
        List<Models.TransaccionSafi> safi;

        try
        {
            using var streamSiap = archivoSiap.OpenReadStream();
            siap = EsXlsx(archivoSiap) ? _siapXlsxReader.Leer(streamSiap) : _siapReader.Leer(streamSiap);

            using var streamSafi = archivoSafi.OpenReadStream();
            safi = EsXlsx(archivoSafi) ? _safiXlsxReader.Leer(streamSafi) : _safiReader.Leer(streamSafi);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"No se pudo leer alguno de los archivos: {ex.Message}");
            return View("Index");
        }

        var resultado = _conciliacionService.Conciliar(siap, safi);

        ViewBag.TotalSiap = siap.Count;
        ViewBag.TotalSafi = safi.Count;

        return View("Resultado", resultado);
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public IActionResult ExportarExcel(string datos)
    {
        var resultados = JsonSerializer.Deserialize<List<Models.ResultadoConciliacion>>(datos);
        if (resultados == null)
            return BadRequest("No se recibió un resultado válido para exportar.");

        var archivo = _resultadoExcelExporter.Exportar(resultados);
        return File(archivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Conciliacion_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }

    private static bool EsXlsx(IFormFile archivo) =>
        Path.GetExtension(archivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
}
