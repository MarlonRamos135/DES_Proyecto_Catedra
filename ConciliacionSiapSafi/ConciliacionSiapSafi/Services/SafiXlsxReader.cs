using ClosedXML.Excel;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class SafiXlsxReader
{
    public List<TransaccionSafi> Leer(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        IXLWorksheet? hoja = null;
        IXLRow? filaEncabezado = null;

        foreach (var hojaActual in libro.Worksheets)
        {
            filaEncabezado = ExcelUtils.BuscarFila(hojaActual, encabezados =>
                encabezados.Count > 0 && encabezados[0].Trim().Equals("Ejercicio", StringComparison.OrdinalIgnoreCase));
            if (filaEncabezado != null)
            {
                hoja = hojaActual;
                break;
            }
        }

        if (hoja == null || filaEncabezado == null)
            throw new InvalidOperationException("No se encontró la fila de encabezado ('Ejercicio') en el archivo XLSX de SAFI.");

        var encabezados = ExcelUtils.ValoresDeFila(filaEncabezado);
        int iCodigo = ExcelUtils.IndiceDe(encabezados, "No. D. Resp", "SAFI");
        int iNit = ExcelUtils.IndiceDe(encabezados, "Nit", "SAFI");
        int iProveedor = ExcelUtils.IndiceDe(encabezados, "Proveedor", "SAFI");
        int iComprometido = ExcelUtils.IndiceDe(encabezados, "Comprometido", "SAFI");

        var resultado = new List<TransaccionSafi>();
        foreach (var fila in hoja.RowsUsed().Where(f => f.RowNumber() > filaEncabezado.RowNumber()))
        {
            var campos = ExcelUtils.ValoresDeFila(fila);
            var codigo = campos.ElementAtOrDefault(iCodigo)?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(codigo)) continue;

            resultado.Add(new TransaccionSafi
            {
                CodigoOriginal = codigo,
                Nit = campos.ElementAtOrDefault(iNit)?.Trim() ?? "",
                Proveedor = campos.ElementAtOrDefault(iProveedor)?.Trim() ?? "",
                Comprometido = CsvUtils.ParseMontoLatino(campos.ElementAtOrDefault(iComprometido) ?? "")
            });
        }

        return resultado;
    }
}