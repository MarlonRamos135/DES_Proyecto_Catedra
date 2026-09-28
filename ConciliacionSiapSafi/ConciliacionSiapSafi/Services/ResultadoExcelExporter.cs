using ClosedXML.Excel;
using ConciliacionSiapSafi.Models;

namespace ConciliacionSiapSafi.Services;

public class ResultadoExcelExporter
{
    public byte[] Exportar(List<ResultadoConciliacion> resultados)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Conciliación");

        var encabezados = new[]
        {
            "Actividad",
            "Descripción",
            "Código SIAP",
            "Código SAFI",
            "Monto SIAP",
            "Monto SAFI",
            "Método",
            "Estado"
        };

        for (int columna = 0; columna < encabezados.Length; columna++)
            hoja.Cell(1, columna + 1).Value = encabezados[columna];

        for (int fila = 0; fila < resultados.Count; fila++)
        {
            var resultado = resultados[fila];
            var filaExcel = fila + 2;

            hoja.Cell(filaExcel, 1).Value = resultado.Actividad;
            hoja.Cell(filaExcel, 2).Value = resultado.ActividadDescripcion;
            hoja.Cell(filaExcel, 3).Value = resultado.CodigoSiap;
            hoja.Cell(filaExcel, 4).Value = resultado.CodigoSafi;
            hoja.Cell(filaExcel, 5).Value = resultado.MontoSiap;
            hoja.Cell(filaExcel, 6).Value = resultado.MontoSafi;
            hoja.Cell(filaExcel, 7).Value = resultado.MetodoVinculo;
            hoja.Cell(filaExcel, 8).Value = resultado.Estado;
        }

        var rango = hoja.Range(1, 1, Math.Max(1, resultados.Count + 1), encabezados.Length);
        rango.CreateTable("TablaConciliacion");
        hoja.Row(1).Style.Font.Bold = true;
        hoja.Columns(5, 6).Style.NumberFormat.Format = "#,##0.00";
        hoja.Columns().AdjustToContents();
        hoja.Column(2).Width = Math.Min(50, Math.Max(20, hoja.Column(2).Width));
        hoja.SheetView.FreezeRows(1);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}