using System.Text.RegularExpressions;

namespace RagBasico.Api;

/// <summary>Corta el texto de un documento en fragmentos por párrafos, de un largo máximo de palabras, con solapamiento.</summary>
public static class Chunker
{
    /// <summary>La versión de la forma de cortar. Entra en la huella de carga: si se cambia cómo se corta, se cambia este texto y la próxima carga recarga todos los PDF.</summary>
    public const string Version = "parrafos-1";

    // Un párrafo con menos palabras que esto, como un título, se pega al párrafo que le sigue.
    private const int PalabrasDeParrafoCorto = 15;

    private static readonly char[] Separadores = new char[] { ' ', '\n', '\r', '\t' };

    // Un párrafo nuevo empieza después de una línea en blanco: dos saltos de línea con, a lo sumo, espacios entre ellos.
    private static readonly Regex LineaEnBlanco = new Regex(@"\n\s*\n");

    /// <summary>Corta el texto en fragmentos de hasta "palabras" palabras, sin partir los párrafos que entran enteros. Cada fragmento repite las últimas "solapamiento" palabras del anterior, para no cortar una idea al medio.</summary>
    public static List<string> Partir(string texto, int palabras, int solapamiento)
    {
        List<string> fragmentos = new List<string>();
        if (palabras < 1)
        {
            palabras = 300;
        }
        if (solapamiento < 0 || solapamiento >= palabras / 2)
        {
            solapamiento = 0;
        }

        // "actual" es el fragmento que se está armando; "nuevas", cuántas de sus palabras no vienen repetidas del anterior.
        List<string> actual = new List<string>();
        int nuevas = 0;
        foreach (string[] parrafo in SepararParrafos(texto))
        {
            // Si el párrafo no entra y el fragmento ya tiene la mitad del tamaño, el fragmento se cierra antes del párrafo.
            // Si tiene menos, el principio del párrafo lo completa: así no quedan fragmentos chicos.
            if (nuevas > 0 && nuevas >= palabras / 2 && actual.Count + parrafo.Length > palabras)
            {
                Cerrar(fragmentos, actual, solapamiento);
                nuevas = 0;
            }
            foreach (string palabra in parrafo)
            {
                actual.Add(palabra);
                nuevas++;
                if (actual.Count >= palabras)
                {
                    Cerrar(fragmentos, actual, solapamiento);
                    nuevas = 0;
                }
            }
        }

        if (nuevas > 0)
        {
            if (fragmentos.Count > 0 && nuevas < solapamiento)
            {
                // Quedaron menos palabras nuevas que repetidas: otro fragmento sería casi una copia del anterior. Se suman al anterior.
                fragmentos[fragmentos.Count - 1] = fragmentos[fragmentos.Count - 1] + " " + string.Join(" ", actual.Skip(actual.Count - nuevas));
            }
            else
            {
                fragmentos.Add(string.Join(" ", actual));
            }
        }
        return fragmentos;
    }

    /// <summary>Cuenta las palabras de un texto.</summary>
    public static int ContarPalabras(string texto)
    {
        return texto.Split(Separadores, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Separa el texto en párrafos por las líneas en blanco y los devuelve como listas de palabras. Un párrafo corto, como un título, se pega al que le sigue.</summary>
    private static List<string[]> SepararParrafos(string texto)
    {
        List<string[]> parrafos = new List<string[]>();
        List<string> pendiente = new List<string>();
        foreach (string bloque in LineaEnBlanco.Split(texto))
        {
            pendiente.AddRange(bloque.Split(Separadores, StringSplitOptions.RemoveEmptyEntries));
            if (pendiente.Count >= PalabrasDeParrafoCorto)
            {
                parrafos.Add(pendiente.ToArray());
                pendiente.Clear();
            }
        }
        // Un párrafo corto al final no tiene a quién pegarse: queda solo, y Partir lo suma al último fragmento.
        if (pendiente.Count > 0)
        {
            parrafos.Add(pendiente.ToArray());
        }
        return parrafos;
    }

    /// <summary>Cierra el fragmento que se está armando: lo agrega a la lista y deja solo sus últimas "solapamiento" palabras, que abren el fragmento siguiente.</summary>
    private static void Cerrar(List<string> fragmentos, List<string> actual, int solapamiento)
    {
        fragmentos.Add(string.Join(" ", actual));
        actual.RemoveRange(0, actual.Count - solapamiento);
    }
}
