using System.Text.RegularExpressions;

namespace RagBasico.Api;

/// <summary>Corta el texto de un documento en fragmentos por párrafos, de un largo máximo de palabras, con solapamiento, y anota en qué página empieza y termina cada uno.</summary>
public static class Chunker
{
    /// <summary>La versión de la forma de cortar. Entra en la huella de carga: si se cambia cómo se corta, se cambia este texto y la próxima carga recarga todos los PDF.</summary>
    // La página de cada fragmento no entra en la versión, porque no cambia el corte: para ponérsela a lo ya cargado, "Reprocesar todo".
    public const string Version = "parrafos-1";

    // Un párrafo con menos palabras que esto, como un título, se pega al párrafo que le sigue.
    private const int PalabrasDeParrafoCorto = 15;

    private static readonly char[] Separadores = new char[] { ' ', '\n', '\r', '\t' };

    // Un párrafo nuevo empieza después de una línea en blanco: dos saltos de línea con, a lo sumo, espacios entre ellos.
    private static readonly Regex LineaEnBlanco = new Regex(@"\n\s*\n");

    /// <summary>Corta un texto suelto en fragmentos, con las mismas reglas que PartirPaginas, y devuelve solo los textos. Es PartirPaginas con una sola página.</summary>
    public static List<string> Partir(string texto, int palabras, int solapamiento)
    {
        List<string> textos = new List<string>();
        foreach (Parte parte in PartirPaginas(new List<string> { texto }, palabras, solapamiento))
        {
            textos.Add(parte.Texto);
        }
        return textos;
    }

    /// <summary>Corta el texto de un PDF, que llega con una página por elemento, en fragmentos de hasta "palabras" palabras, sin partir los párrafos que entran enteros. Cada fragmento repite las últimas "solapamiento" palabras del anterior, para no cortar una idea al medio, y sale con la página donde está su primera palabra y la página donde está la última.</summary>
    public static List<Parte> PartirPaginas(List<string> paginas, int palabras, int solapamiento)
    {
        List<Parte> fragmentos = new List<Parte>();
        if (palabras < 1)
        {
            palabras = 300;
        }
        if (solapamiento < 0 || solapamiento >= palabras / 2)
        {
            solapamiento = 0;
        }

        // "actual" es el fragmento que se está armando; "nuevas", cuántas de sus palabras no vienen repetidas del anterior.
        List<Palabra> actual = new List<Palabra>();
        int nuevas = 0;
        foreach (Palabra[] parrafo in SepararParrafos(paginas))
        {
            // Si el párrafo no entra y el fragmento ya tiene la mitad del tamaño, el fragmento se cierra antes del párrafo.
            // Si tiene menos, el principio del párrafo lo completa: así no quedan fragmentos chicos.
            if (nuevas > 0 && nuevas >= palabras / 2 && actual.Count + parrafo.Length > palabras)
            {
                Cerrar(fragmentos, actual, solapamiento);
                nuevas = 0;
            }
            foreach (Palabra palabra in parrafo)
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
                // Quedaron menos palabras nuevas que repetidas: otro fragmento sería casi una copia del anterior. Se suman al anterior,
                // que conserva su página de inicio y pasa a terminar en la página de la última palabra sumada.
                Parte anterior = fragmentos[fragmentos.Count - 1];
                List<Palabra> cola = actual.Skip(actual.Count - nuevas).ToList();
                fragmentos[fragmentos.Count - 1] = new Parte(anterior.Texto + " " + Unir(cola), anterior.Pagina, cola[cola.Count - 1].Pagina);
            }
            else
            {
                fragmentos.Add(ArmarParte(actual));
            }
        }
        return fragmentos;
    }

    /// <summary>Cuenta las palabras de un texto.</summary>
    public static int ContarPalabras(string texto)
    {
        return texto.Split(Separadores, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Separa el texto de las páginas en párrafos por las líneas en blanco y los devuelve como listas de palabras, cada palabra con el número de su página. Un párrafo corto, como un título, se pega al que le sigue, aunque el que le sigue esté en la página siguiente.</summary>
    private static List<Palabra[]> SepararParrafos(List<string> paginas)
    {
        List<Palabra[]> parrafos = new List<Palabra[]>();
        // "pendiente" no se vacía al cambiar de página: un título al pie de una página se pega al párrafo que abre la siguiente.
        List<Palabra> pendiente = new List<Palabra>();
        for (int i = 0; i < paginas.Count; i++)
        {
            // Las páginas se numeran desde 1, en el orden del PDF: es el número que usa el visor del navegador.
            int numero = i + 1;
            foreach (string bloque in LineaEnBlanco.Split(paginas[i]))
            {
                foreach (string palabra in bloque.Split(Separadores, StringSplitOptions.RemoveEmptyEntries))
                {
                    pendiente.Add(new Palabra(palabra, numero));
                }
                if (pendiente.Count >= PalabrasDeParrafoCorto)
                {
                    parrafos.Add(pendiente.ToArray());
                    pendiente.Clear();
                }
            }
        }
        // Un párrafo corto al final no tiene a quién pegarse: queda solo, y PartirPaginas lo suma al último fragmento.
        if (pendiente.Count > 0)
        {
            parrafos.Add(pendiente.ToArray());
        }
        return parrafos;
    }

    /// <summary>Cierra el fragmento que se está armando: lo agrega a la lista y deja solo sus últimas "solapamiento" palabras, que abren el fragmento siguiente.</summary>
    private static void Cerrar(List<Parte> fragmentos, List<Palabra> actual, int solapamiento)
    {
        fragmentos.Add(ArmarParte(actual));
        actual.RemoveRange(0, actual.Count - solapamiento);
    }

    /// <summary>Arma un fragmento con sus palabras: el texto, la página de la primera palabra y la página de la última.</summary>
    private static Parte ArmarParte(List<Palabra> palabras)
    {
        return new Parte(Unir(palabras), palabras[0].Pagina, palabras[palabras.Count - 1].Pagina);
    }

    /// <summary>Une las palabras con un espacio entre una y otra.</summary>
    private static string Unir(List<Palabra> palabras)
    {
        return string.Join(" ", palabras.Select(palabra => palabra.Texto));
    }

    /// <summary>Una palabra del texto con el número de la página donde está.</summary>
    private sealed record Palabra(string Texto, int Pagina);
}
