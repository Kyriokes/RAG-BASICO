namespace RagBasico.Api;

/// <summary>Decide qué hacer con cada PDF comparando la carpeta con la base, sin tocar ninguna de las dos.</summary>
public static class Planificador
{
    /// <summary>Compara los PDF de la carpeta con los documentos cargados y arma el plan: cuáles cargar porque son nuevos, cuáles porque cambiaron, cuáles quedan igual y cuáles borrar de la base. Si la carpeta no tiene ningún PDF, no borra nada.</summary>
    public static PlanDeCarga Armar(List<ArchivoEnCarpeta> enCarpeta, List<DocumentoCargado> enBase)
    {
        // Windows no distingue mayúsculas en los nombres de archivo, y SQL Server tampoco al comparar: acá tampoco.
        Dictionary<string, DocumentoCargado> cargados = new Dictionary<string, DocumentoCargado>(StringComparer.OrdinalIgnoreCase);
        foreach (DocumentoCargado documento in enBase)
        {
            cargados[documento.Archivo] = documento;
        }

        List<ArchivoEnCarpeta> nuevos = new List<ArchivoEnCarpeta>();
        List<ArchivoEnCarpeta> cambiados = new List<ArchivoEnCarpeta>();
        List<ArchivoEnCarpeta> sinCambios = new List<ArchivoEnCarpeta>();
        HashSet<string> nombresEnCarpeta = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ArchivoEnCarpeta archivo in enCarpeta)
        {
            nombresEnCarpeta.Add(archivo.Archivo);
            if (archivo.Huella == null)
            {
                // No se pudo leer: no se carga, y como está en la carpeta tampoco se borra. Queda lo que había.
                continue;
            }
            DocumentoCargado? documento;
            if (!cargados.TryGetValue(archivo.Archivo, out documento))
            {
                nuevos.Add(archivo);
            }
            else if (documento.Huella != null && documento.Huella.SequenceEqual(archivo.Huella))
            {
                sinCambios.Add(archivo);
            }
            else
            {
                cambiados.Add(archivo);
            }
        }

        // La regla de la carpeta vacía: una carpeta sin ningún PDF puede ser un disco desconectado o una carpeta equivocada.
        // Borrar todo por eso sería un desastre, así que en ese caso no se borra nada.
        List<DocumentoCargado> borrar = new List<DocumentoCargado>();
        if (enCarpeta.Count > 0)
        {
            foreach (DocumentoCargado documento in enBase)
            {
                if (!nombresEnCarpeta.Contains(documento.Archivo))
                {
                    borrar.Add(documento);
                }
            }
        }
        return new PlanDeCarga(nuevos, cambiados, sinCambios, borrar);
    }
}
