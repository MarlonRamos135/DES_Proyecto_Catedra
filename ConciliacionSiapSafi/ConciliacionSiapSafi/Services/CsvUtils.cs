namespace ConciliacionSiapSafi.Services;

// Parser CSV simple con soporte de comillas (RFC4180 básico).
// No soporta campos con saltos de línea dentro de comillas.
public static class CsvUtils
{
    public static List<string> SplitLine(string line, char delimitador)
    {
        var campos = new List<string>();
        var actual = new System.Text.StringBuilder();
        bool dentroDeComillas = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (dentroDeComillas)
            {
                if (c == '"')
                {
                    // comilla escapada ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else
                    {
                        dentroDeComillas = false;
                    }
                }
                else
                {
                    actual.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    dentroDeComillas = true;
                }
                else if (c == delimitador)
                {
                    campos.Add(actual.ToString());
                    actual.Clear();
                }
                else
                {
                    actual.Append(c);
                }
            }
        }
        campos.Add(actual.ToString());
        return campos;
    }

    // Convierte un monto en formato latino: " $3.155.636,00 " -> 3155636.00m
    // También maneja el caso de vacío/"-" como cero.
    public static decimal ParseMontoLatino(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return 0m;

        var v = valor.Trim();
        v = v.Replace("$", "").Trim();

        if (v == "-" || v.Length == 0) return 0m;

        v = v.Replace(".", "");   // quita separador de miles
        v = v.Replace(",", "."); // convierte separador decimal

        return decimal.TryParse(v, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var resultado)
            ? resultado
            : 0m;
    }
}
