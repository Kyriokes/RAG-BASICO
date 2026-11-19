# Erratas

Bugs encontrados después de cerrar una fase, y dónde se arreglaron.

Este archivo solo se actualiza en la punta. Para ver las erratas de una fase vieja, mirá este archivo en la branch de fase más nueva.

| Fase | Archivo | Función | Síntoma | Arreglo |
|---|---|---|---|---|
| fase-5-vectores | `RagBasico.Api\Vectorizador.cs` | `CalcularPendientesAsync` | Si fallaban todos los fragmentos de un lote por su contenido, `/vectores` cortaba la corrida culpando a Ollama, y como cada corrida empieza de nuevo, se trababa siempre en el mismo lote: los fragmentos de después no tenían vector nunca | En fase-7-qdrant: corta solo si Ollama no responde, o si fallan cinco lotes seguidos |
