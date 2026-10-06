using ClosedXML.Excel;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class SafiXlsxReader
{
    public List<TransaccionSafi> Leer(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var comprometido = LeerHoja(libro, "Comprometido");
        var devengado = LeerHoja(libro, "Devengado");
        var pagado = LeerHoja(libro, "Pagado");

        var devPorCompromiso = devengado
            .GroupBy(x => x.Compromiso, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => new
            {
                NoDocuOrig = string.Join(" / ", g.Select(x => x.NoDocuOrig).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)),
                Devengado = g.Sum(x => x.Devengado)
            }, StringComparer.OrdinalIgnoreCase);

        var pagadoPorDocumento = pagado
            .GroupBy(x => x.NoDocuOrig, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Pagado), StringComparer.OrdinalIgnoreCase);

        foreach (var transaccion in comprometido)
        {
            if (devPorCompromiso.TryGetValue(transaccion.NoCompro, out var dev))
            {
                transaccion.NoDocuOrig = dev.NoDocuOrig;
                transaccion.Devengado = dev.Devengado;
            }

            transaccion.Pagado = transaccion.NoDocuOrig
                .Split(" / ", StringSplitOptions.RemoveEmptyEntries)
                .Sum(documento => pagadoPorDocumento.TryGetValue(documento, out var monto) ? monto : 0m);
        }

        return comprometido;
    }

    private static List<TransaccionSafi> LeerHoja(XLWorkbook libro, string nombreHoja)
    {
        var hoja = libro.Worksheets.FirstOrDefault(x =>
            x.Name.Trim().Equals(nombreHoja, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No se encontró la hoja '{nombreHoja}' en el archivo XLSX de SAFI.");

        var filaEncabezado = ExcelUtils.BuscarFila(hoja, encabezados =>
            encabezados.Any(h => h.Trim().Equals("Ejercicio", StringComparison.OrdinalIgnoreCase)));
        if (filaEncabezado == null)
            throw new InvalidOperationException($"No se encontró el encabezado en la hoja '{nombreHoja}' del archivo SAFI.");

        var encabezados = ExcelUtils.ValoresDeFila(filaEncabezado);
        int? IndiceOpcional(params string[] nombres) => ExcelUtils.IndiceOpcional(encabezados, nombres);

        var indiceCompromiso = IndiceOpcional("Compromiso");
        var indiceNoCompro = IndiceOpcional("N. Compro", "No. Compro", "No. D. Resp");
        var indiceNoDocuOrig = IndiceOpcional("No. Docu. Orig", "No. Documento Orig");
        var indiceNoDResp = IndiceOpcional("No. D. Resp");
        var indiceNit = IndiceOpcional("Nit");
        var indiceProveedor = IndiceOpcional("Proveedor");
        var indiceFF = IndiceOpcional("F.F.");
        var indiceProy = IndiceOpcional("Proy.");
        var indiceFR = IndiceOpcional("F.R.");
        var indiceAO = IndiceOpcional("A.O.");
        var indiceComprometido = IndiceOpcional("Comprometido");
        var indiceDescomp = IndiceOpcional("Descomp");
        var indiceConcepto = IndiceOpcional("Concepto");
        var indiceDevengado = IndiceOpcional("Devengado");
        var indicePagado = IndiceOpcional("Pagado");

        var resultado = new List<TransaccionSafi>();
        foreach (var fila in hoja.RowsUsed().Where(f => f.RowNumber() > filaEncabezado.RowNumber()))
        {
            var campos = ExcelUtils.ValoresDeFila(fila);
            string Valor(int? indice) => indice.HasValue ? campos.ElementAtOrDefault(indice.Value)?.Trim() ?? "" : "";
            var codigo = Valor(indiceNoDResp);
            var noCompro = Valor(indiceNoCompro);
            var compromiso = Valor(indiceCompromiso);
            if (nombreHoja.Equals("Comprometido", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(codigo)) continue;

            resultado.Add(new TransaccionSafi
            {
                NoDResp = codigo,
                NoCompro = noCompro,
                Compromiso = compromiso,
                NoDocuOrig = Valor(indiceNoDocuOrig),
                Nit = Valor(indiceNit),
                Proveedor = Valor(indiceProveedor),
                FF = Valor(indiceFF),
                Proy = Valor(indiceProy),
                FR = Valor(indiceFR),
                AO = Valor(indiceAO),
                Comprometido = CsvUtils.ParseMontoLatino(Valor(indiceComprometido)),
                Descomp = CsvUtils.ParseMontoLatino(Valor(indiceDescomp)),
                Concepto = Valor(indiceConcepto),
                Devengado = CsvUtils.ParseMontoLatino(Valor(indiceDevengado)),
                Pagado = CsvUtils.ParseMontoLatino(Valor(indicePagado)),
                // La hoja Devengado relaciona por "Compromiso".
                CodigoOriginal = nombreHoja.Equals("Devengado", StringComparison.OrdinalIgnoreCase) ? compromiso : codigo
            });
        }

        return resultado;
    }
}
