using Microsoft.AspNetCore.Mvc;
using ConciliacionSiapSafi.Services;

namespace ConciliacionSiapSafi.Controllers;

public class ConciliacionController : Controller
{
    private readonly SiapCsvReader _siapReader;
    private readonly SafiCsvReader _safiReader;
    private readonly ConciliacionService _conciliacionService;

    public ConciliacionController(SiapCsvReader siapReader, SafiCsvReader safiReader, ConciliacionService conciliacionService)
    {
        _siapReader = siapReader;
        _safiReader = safiReader;
        _conciliacionService = conciliacionService;
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
            ModelState.AddModelError("", "Debe subir ambos archivos (SIAP y SAFI), en formato .csv.");
            return View("Index");
        }

        List<Models.ContratoSiap> siap;
        List<Models.TransaccionSafi> safi;

        try
        {
            using var streamSiap = archivoSiap.OpenReadStream();
            siap = _siapReader.Leer(streamSiap);

            using var streamSafi = archivoSafi.OpenReadStream();
            safi = _safiReader.Leer(streamSafi);
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
}
