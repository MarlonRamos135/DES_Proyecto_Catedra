using System.Text;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class SafiCsvReader
{
    // El archivo real trae varias filas de título/metadatos antes del encabezado
    // (ej. "INFORME DE COMPROMISOS", "Institución: ...", "Unidad Financiera: ...").
    // Se busca la fila que empieza con 'Ejercicio' para ubicar el encabezado real.
    public List<TransaccionSafi> Leer(Stream archivo)
    {
        var resultado = new List<TransaccionSafi>();
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        using var reader = new StreamReader(archivo, encoding);

        List<string>? encabezados = null;
        string? linea;

        while ((linea = reader.ReadLine()) != null)
        {
            var campos = CsvUtils.SplitLine(linea, ';');
            if (campos.Count > 0 && campos[0].Trim().Equals("Ejercicio", StringComparison.OrdinalIgnoreCase))
            {
                encabezados = campos;
                break;
            }
        }

        if (encabezados == null)
            throw new InvalidOperationException("No se encontró la fila de encabezado ('Ejercicio') en el archivo de SAFI.");

        int IndiceDe(string nombreColumna)
        {
            var idx = encabezados.FindIndex(h => h.Trim().Equals(nombreColumna, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new InvalidOperationException($"No se encontró la columna '{nombreColumna}' en el archivo de SAFI.");
            return idx;
        }

        int iCodigo = IndiceDe("No. D. Resp");
        int iNit = IndiceDe("Nit");
        int iProveedor = IndiceDe("Proveedor");
        int iComprometido = IndiceDe("Comprometido"); // el encabezado real trae espacios: " Comprometido "

        while ((linea = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = CsvUtils.SplitLine(linea, ';');
            if (campos.Count <= iComprometido) continue;

            var codigo = campos.ElementAtOrDefault(iCodigo)?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(codigo)) continue; // filas de totales u otras sin documento

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
