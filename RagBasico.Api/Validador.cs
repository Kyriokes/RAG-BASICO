namespace RagBasico.Api;

/// <summary>Controla lo que llega en un pedido antes de usarlo, para responder un error claro en lugar de buscar con algo que no sirve.</summary>
public static class Validador
{
    /// <summary>Controla una pregunta: que no esté vacía, que no pase el máximo de caracteres, que tenga alguna letra o número y que no tenga caracteres de control. Devuelve el motivo del rechazo, o vacío si la pregunta está bien.</summary>
    public static string RevisarPregunta(string? pregunta, int maximo)
    {
        if (pregunta == null || pregunta.Trim().Length == 0)
        {
            return "La pregunta está vacía.";
        }
        if (pregunta.Length > maximo)
        {
            return "La pregunta tiene " + pregunta.Length + " caracteres, y el máximo es " + maximo + ". Hacela más corta.";
        }

        bool tieneLetras = false;
        foreach (char letra in pregunta)
        {
            // Los saltos de línea y las tabulaciones se aceptan; los demás caracteres de control no se escriben con el teclado.
            if (char.IsControl(letra) && letra != '\n' && letra != '\r' && letra != '\t')
            {
                return "La pregunta tiene caracteres de control, que no se escriben con el teclado.";
            }
            if (char.IsLetterOrDigit(letra))
            {
                tieneLetras = true;
            }
        }
        if (!tieneLetras)
        {
            return "La pregunta no tiene letras ni números.";
        }
        return "";
    }
}
