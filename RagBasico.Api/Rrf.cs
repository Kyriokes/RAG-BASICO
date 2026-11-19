namespace RagBasico.Api;

/// <summary>Mezcla listas de resultados por puesto, con RRF (Reciprocal Rank Fusion): no mira los puntajes de cada búsqueda, que no se pueden comparar entre sí, sino en qué puesto quedó cada fragmento.</summary>
public static class Rrf
{
    /// <summary>La constante k de RRF. 60 es el valor del trabajo original y el que usa casi todo el mundo: achica la diferencia entre el primer puesto y los siguientes, así un fragmento que aparece bien en las dos listas le gana a uno que es primero en una sola.</summary>
    public const int K = 60;

    /// <summary>Mezcla las listas: cada fragmento suma 1 / (K + puesto) por cada lista en la que aparece, con el puesto contado desde 1. Devuelve los topK de mayor puntaje; si dos empatan, queda primero el que apareció antes.</summary>
    public static List<Fragmento> Mezclar(List<List<Fragmento>> listas, int topK)
    {
        Dictionary<int, double> puntajes = new Dictionary<int, double>();
        Dictionary<int, Fragmento> fragmentos = new Dictionary<int, Fragmento>();
        // El orden en que fue apareciendo cada fragmento: sirve para desempatar.
        List<int> orden = new List<int>();

        foreach (List<Fragmento> lista in listas)
        {
            for (int i = 0; i < lista.Count; i++)
            {
                int id = lista[i].FragmentoId;
                if (!puntajes.ContainsKey(id))
                {
                    puntajes[id] = 0;
                    fragmentos[id] = lista[i];
                    orden.Add(id);
                }
                int puesto = i + 1;
                puntajes[id] = puntajes[id] + 1.0 / (K + puesto);
            }
        }

        // OrderByDescending es estable: con el mismo puntaje, respeta el orden de la lista "orden".
        return orden
            .OrderByDescending(id => puntajes[id])
            .Take(topK)
            .Select(id => fragmentos[id])
            .ToList();
    }
}
