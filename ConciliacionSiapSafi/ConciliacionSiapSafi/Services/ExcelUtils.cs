using ClosedXML.Excel;

namespace ConciliacionSiapSafi.Services;

public static class ExcelUtils
{
    public static List<string> ValoresDeFila(IXLRow fila)
    {
        var ultimaColumna = fila.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var valores = new List<string>(ultimaColumna);

        for (int columna = 1; columna <= ultimaColumna; columna++)
            valores.Add(fila.Cell(columna).GetString());

        return valores;
    }

    public static IXLRow? BuscarFila(IXLWorksheet hoja, Func<List<string>, bool> coincide)
    {
        foreach (var fila in hoja.RowsUsed())
        {
            if (coincide(ValoresDeFila(fila)))
                return fila;
        }

        return null;
    }

    public static int IndiceDe(List<string> encabezados, string nombreColumna, string fuente)
    {
        var indice = encabezados.FindIndex(h => h.Trim().Equals(nombreColumna, StringComparison.OrdinalIgnoreCase));
        if (indice < 0)
            throw new InvalidOperationException($"No se encontró la columna '{nombreColumna}' en el archivo de {fuente}.");

        return indice;
    }
}