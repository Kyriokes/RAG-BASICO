namespace RagBasico.Api;

/// <summary>Corta un texto en fragmentos de un largo fijo de palabras, con solapamiento.</summary>
public static class Chunker
{
    private static readonly char[] Separadores = new char[] { ' ', '\n', '\r', '\t' };

    /// <summary>Corta el texto en ventanas de "palabras" palabras. Cada ventana repite las últimas "solapamiento" palabras de la anterior, para no cortar una idea al medio.</summary>
    public static List<string> Partir(string texto, int palabras, int solapamiento)
    {
        List<string> fragmentos = new List<string>();
        string[] todas = texto.Split(Separadores, StringSplitOptions.RemoveEmptyEntries);
        if (todas.Length == 0)
        {
            return fragmentos;
        }
        if (palabras < 1)
        {
            palabras = 300;
        }
        if (solapamiento < 0 || solapamiento >= palabras)
        {
            solapamiento = 0;
        }

        int paso = palabras - solapamiento;
        for (int inicio = 0; inicio < todas.Length; inicio += paso)
        {
            int cantidad = Math.Min(palabras, todas.Length - inicio);
            fragmentos.Add(string.Join(" ", todas, inicio, cantidad));
            if (inicio + cantidad >= todas.Length)
            {
                break;
            }
        }
        return fragmentos;
    }

    /// <summary>Cuenta las palabras de un texto.</summary>
    public static int ContarPalabras(string texto)
    {
        return texto.Split(Separadores, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
