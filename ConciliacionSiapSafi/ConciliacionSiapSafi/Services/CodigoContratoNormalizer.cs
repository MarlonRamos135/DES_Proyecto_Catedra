using System.Text.RegularExpressions;

namespace ConciliacionSiapSafi.Services;

public record ResultadoNormalizacion(bool TienePatron, string CodigoNormalizado);

// Extrae el patrón "número/año" de en medio del ruido (prefijos con errores de
// digitación como MINEDUCTY, MINEDUCUYT, IMINEDUCYT; sufijos como -BIRF, " BIRF 9067 SV";
// puntos sueltos al inicio o al final) en vez de intentar limpiar el prefijo exacto.
//
// Ejemplos que debe unificar en "66/2024":
//   MINEDUCYT-66/2024-BIRF
//   MINEDCUYT-66/2024-BIRF          (typo)
//   .MINEDUCYT-66/2024-BIRF
//   MINEDUCYT 66/2024 BIRF 9067 SV
//
// Códigos sin el patrón número/año (ej. "123917") se marcan con TienePatron = false;
// esos se resuelven después por otra vía (proveedor + monto), no por código.
public class CodigoContratoNormalizer
{
    private static readonly Regex PatronNumeroAnio = new(@"(\d{1,3})\s*/\s*(\d{4})", RegexOptions.Compiled);

    public ResultadoNormalizacion Normalizar(string codigoOriginal)
    {
        if (string.IsNullOrWhiteSpace(codigoOriginal))
            return new ResultadoNormalizacion(false, "");

        var texto = codigoOriginal.Trim().ToUpperInvariant();
        var m = PatronNumeroAnio.Match(texto);

        if (m.Success)
        {
            var numero = m.Groups[1].Value.PadLeft(2, '0');
            var anio = m.Groups[2].Value;
            return new ResultadoNormalizacion(true, $"{numero}/{anio}");
        }

        // Sin patrón número/año reconocible: se conserva el texto crudo (sin espacios extra)
        // para agrupar transacciones idénticas, pero no se usará para vincular por código.
        return new ResultadoNormalizacion(false, texto);
    }

    // En SIAP, el 58% de los códigos vienen sin año (solo "07", "100", "11"...) porque
    // el año ya está en la columna 'Año fiscal' de la misma fila. Si el código no trae
    // el patrón número/año, se reconstruye combinando el número con ese año fiscal.
    // Esto sube la tasa de coincidencias por código de 42% a 60% sobre datos reales.
    public ResultadoNormalizacion NormalizarConAnioFiscal(string codigoOriginal, string anioFiscal)
    {
        var directo = Normalizar(codigoOriginal);
        if (directo.TienePatron) return directo;

        var soloDigitos = new string(codigoOriginal.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length > 0 && anioFiscal?.Trim().Length == 4)
        {
            var numero = soloDigitos.PadLeft(2, '0');
            return new ResultadoNormalizacion(true, $"{numero}/{anioFiscal.Trim()}");
        }

        return directo; // no se pudo reconstruir, queda sin patrón
    }
}
