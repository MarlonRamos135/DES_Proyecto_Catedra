using System.Text;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class SiapCsvReader
{
    // El archivo real viene en ISO-8859-1 (Latin1), separado por ';', con encabezado en la fila 1.
    // Solo interesan las filas donde 'Número de contrato' no viene vacío.
    public List<ContratoSiap> Leer(Stream archivo)
    {
        var resultado = new List<ContratoSiap>();
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        using var reader = new StreamReader(archivo, encoding);

        var lineaEncabezado = reader.ReadLine();
        if (lineaEncabezado == null) return resultado;

        var delimitador = CsvUtils.DetectDelimiter(lineaEncabezado);
        var encabezados = CsvUtils.SplitLine(lineaEncabezado, delimitador);

        int IndiceDe(string nombreColumna)
        {
            var idx = encabezados.FindIndex(h => h.Trim().Equals(nombreColumna, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new InvalidOperationException($"No se encontró la columna '{nombreColumna}' en el archivo de SIAP.");
            return idx;
        }

        int iActividad = IndiceDe("Actividad");
        int iActividadDesc = IndiceDe("Actividad descripción");
        int iNumeroContrato = IndiceDe("Número de contrato");
        int iAnioFiscal = IndiceDe("Año fiscal");
        int iMontoContrato = IndiceDe("Monto de Contrato");
        int iNit = IndiceDe("Nit proveedor");
        int iNombreProveedor = IndiceDe("Nombre proveedor");

        string? linea;
        while ((linea = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = CsvUtils.SplitLine(linea, delimitador);
            if (campos.Count <= iNumeroContrato) continue;

            var numeroContrato = campos[iNumeroContrato].Trim();
            if (string.IsNullOrWhiteSpace(numeroContrato)) continue; // sin contrato asociado, se ignora

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
