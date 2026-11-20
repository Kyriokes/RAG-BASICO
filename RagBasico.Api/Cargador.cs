using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RagBasico.Api;

/// <summary>Procesa un documento pendiente de punta a punta: saca el texto del PDF, lo corta, guarda los fragmentos, calcula sus vectores y los sube a Qdrant.</summary>
public sealed class Cargador
{
    // Un PDF con texto tiene cientos de palabras por página. Menos de esto en promedio, es un escaneo.
    private const int PalabrasPorPaginaMinimas = 40;

    /// <summary>Saca el texto de un PDF con PdfPig, una página por elemento de la lista, en el orden en que se lee. Si el PDF está roto o tiene clave, lanza el error de PdfPig.</summary>
    public static List<string> SacarTexto(byte[] contenido)
    {
        List<string> paginas = new List<string>();
        using (PdfDocument pdf = PdfDocument.Open(contenido))
        {
            foreach (Page pagina in pdf.GetPages())
            {
                paginas.Add(ContentOrderTextExtractor.GetText(pagina, true));
            }
        }
        return paginas;
    }

    /// <summary>Dice si un PDF parece escaneado: menos de 40 palabras por página en promedio. Un escaneo con un sello de firma por página tiene algunas palabras, pero no tantas.</summary>
    public static bool EsEscaneado(List<string> paginas)
    {
        if (paginas.Count == 0)
        {
            return true;
        }
        int palabras = 0;
        foreach (string pagina in paginas)
        {
            palabras += Chunker.ContarPalabras(pagina);
        }
        return palabras < PalabrasPorPaginaMinimas * paginas.Count;
    }

    /// <summary>Busca señales de que el texto salió mal del PDF: caracteres que no son letras ni números, palabras pegadas o páginas casi vacías. Devuelve el motivo de la duda, o vacío si el texto parece bien.</summary>
    public static string RevisarTexto(List<string> paginas)
    {
        int palabras = 0;
        int caracteres = 0;
        int letrasYNumeros = 0;
        int paginasCasiVacias = 0;
        foreach (string pagina in paginas)
        {
            int palabrasDeLaPagina = Chunker.ContarPalabras(pagina);
            palabras += palabrasDeLaPagina;
            if (palabrasDeLaPagina < 20)
            {
                paginasCasiVacias++;
            }
            foreach (char letra in pagina)
            {
                if (!char.IsWhiteSpace(letra))
                {
                    caracteres++;
                    if (char.IsLetterOrDigit(letra))
                    {
                        letrasYNumeros++;
                    }
                }
            }
        }
        if (palabras == 0)
        {
            return "no tiene texto";
        }

        // En un texto normal, casi todo lo que no es espacio son letras y números. Si no, la fuente del PDF no se pudo leer bien.
        int porcentaje = letrasYNumeros * 100 / caracteres;
        if (porcentaje < 80)
        {
            return "solo el " + porcentaje + " % de los caracteres son letras o números: ¿la fuente del PDF no se puede leer?";
        }
        // Una palabra en castellano tiene unas 5 letras. Si el promedio pasa de 12, el PDF no marca los espacios.
        int largo = caracteres / palabras;
        if (largo > 12)
        {
            return "palabras pegadas, " + largo + " caracteres por palabra en promedio";
        }
        // Una página escaneada adentro de un PDF con texto, como un anexo, queda casi vacía.
        if (paginasCasiVacias * 4 >= paginas.Count)
        {
            return paginasCasiVacias + " de " + paginas.Count + " páginas casi sin texto: ¿tiene partes escaneadas?";
        }
        return "";
    }
}
