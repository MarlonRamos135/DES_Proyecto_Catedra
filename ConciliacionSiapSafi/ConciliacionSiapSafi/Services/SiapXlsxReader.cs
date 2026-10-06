using ClosedXML.Excel;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class SiapXlsxReader
{
    public List<ContratoSiap> Leer(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var hoja = libro.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("El archivo XLSX de SIAP no contiene hojas.");

        var filaEncabezado = ExcelUtils.BuscarFila(hoja, encabezados =>
            encabezados.Any(h => h.Trim().Equals("Número de contrato", StringComparison.OrdinalIgnoreCase)));

        if (filaEncabezado == null)
            throw new InvalidOperationException("No se encontró la fila de encabezado en el archivo XLSX de SIAP.");

        var encabezados = ExcelUtils.ValoresDeFila(filaEncabezado);
        int iActividad = ExcelUtils.IndiceDe(encabezados, "Actividad", "SIAP");
        int iActividadDesc = ExcelUtils.IndiceDe(encabezados, "Actividad descripción", "SIAP");
        int iNumeroContrato = ExcelUtils.IndiceDe(encabezados, "Número de contrato", "SIAP");
        int iAnioFiscal = ExcelUtils.IndiceDe(encabezados, "Año fiscal", "SIAP");
        int iMontoContrato = ExcelUtils.IndiceDe(encabezados, "Monto de Contrato", "SIAP");
        int iNit = ExcelUtils.IndiceDe(encabezados, "Nit proveedor", "SIAP");
        int iNombreProveedor = ExcelUtils.IndiceDe(encabezados, "Nombre proveedor", "SIAP");

        var resultado = new List<ContratoSiap>();
        foreach (var fila in hoja.RowsUsed().Where(f => f.RowNumber() > filaEncabezado.RowNumber()))
        {
            var campos = ExcelUtils.ValoresDeFila(fila);
            var numeroContrato = campos.ElementAtOrDefault(iNumeroContrato)?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(numeroContrato)) continue;

            decimal.TryParse(campos.ElementAtOrDefault(iMontoContrato)?.Trim(), out var monto);
            var columnas = encabezados
                .Select((encabezado, indice) => new { encabezado = encabezado.Trim(), indice })
                .Where(x => !string.IsNullOrWhiteSpace(x.encabezado))
                .ToDictionary(x => x.encabezado, x => campos.ElementAtOrDefault(x.indice)?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);
            resultado.Add(new ContratoSiap
            {
                Columnas = columnas,
                Actividad = campos.ElementAtOrDefault(iActividad)?.Trim() ?? "",
                ActividadDescripcion = campos.ElementAtOrDefault(iActividadDesc)?.Trim() ?? "",
                NumeroContratoOriginal = numeroContrato,
                AnioFiscal = campos.ElementAtOrDefault(iAnioFiscal)?.Trim() ?? "",
                MontoContrato = monto,
                NitProveedor = campos.ElementAtOrDefault(iNit)?.Trim() ?? "",
                NombreProveedor = campos.ElementAtOrDefault(iNombreProveedor)?.Trim() ?? ""
            });
        }

        return resultado;
    }
}