using ClosedXML.Excel;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class ResultadoExcelExporter
{
    public byte[] Exportar(List<ResultadoConciliacion> resultados)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Conciliación");
        var encabezadosSiap = resultados
            .SelectMany(x => x.ColumnasSiap.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var encabezados = new List<string>(encabezadosSiap);

        AgregarEncabezado("N. Compro");
        AgregarEncabezado("No. Docu. Orig");
        AgregarEncabezado("No. D. Resp");
        AgregarEncabezado("Nit");
        AgregarEncabezado("Proveedor");
        AgregarEncabezado("F.F.");
        AgregarEncabezado("Proy.");
        AgregarEncabezado("F.R.");
        AgregarEncabezado("A.O.");
        AgregarEncabezado("Comprometido SIAP");
        AgregarEncabezado("Comprometido SAFI");
        AgregarEncabezado("Descomp");
        AgregarEncabezado("Concepto");
        AgregarEncabezado("Devengado");
        AgregarEncabezado("Pagado");
        AgregarEncabezado("Método vínculo");
        AgregarEncabezado("Estado");

        for (int columna = 0; columna < encabezados.Count; columna++)
            hoja.Cell(1, columna + 1).Value = encabezados[columna];

        for (int fila = 0; fila < resultados.Count; fila++)
        {
            var resultado = resultados[fila];
            var filaExcel = fila + 2;
            for (int columna = 0; columna < encabezadosSiap.Count; columna++)
            {
                var nombre = encabezadosSiap[columna];
                hoja.Cell(filaExcel, columna + 1).Value =
                    resultado.ColumnasSiap.TryGetValue(nombre, out var valor) ? valor : "";
            }

            var valoresSafi = new object[]
            {
                resultado.NoCompro, resultado.NoDocuOrig, resultado.NoDResp,
                resultado.Nit, resultado.Proveedor, resultado.FF, resultado.Proy,
                resultado.FR, resultado.AO, resultado.MontoSiap, resultado.ComprometidoSafi, resultado.Descomp,
                resultado.Concepto, resultado.Devengado, resultado.Pagado,
                resultado.MetodoVinculo, resultado.Estado
            };
            for (int columna = 0; columna < valoresSafi.Length; columna++)
            {
                var celda = hoja.Cell(filaExcel, encabezadosSiap.Count + columna + 1);
                if (valoresSafi[columna] is decimal monto)
                    celda.Value = monto;
                else
                    celda.Value = valoresSafi[columna]?.ToString() ?? "";
            }
        }

        var rango = hoja.Range(1, 1, Math.Max(1, resultados.Count + 1), encabezados.Count);
        rango.CreateTable("TablaConciliacion");
        hoja.Row(1).Style.Font.Bold = true;
        hoja.Columns(encabezadosSiap.Count + 10, encabezadosSiap.Count + 12)
            .Style.NumberFormat.Format = "#,##0.00";
        hoja.Columns(encabezadosSiap.Count + 14, encabezadosSiap.Count + 15)
            .Style.NumberFormat.Format = "#,##0.00";
        hoja.Columns().AdjustToContents();
        hoja.Column(encabezados.IndexOf("Concepto") + 1).Width = 50;
        hoja.SheetView.FreezeRows(1);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();

        void AgregarEncabezado(string encabezado)
        {
            var original = encabezado;
            var numero = 2;
            while (encabezados.Contains(encabezado, StringComparer.OrdinalIgnoreCase))
                encabezado = $"{original} ({numero++})";
            encabezados.Add(encabezado);
        }
    }
}
