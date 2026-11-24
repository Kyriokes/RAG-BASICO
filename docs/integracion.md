# Conectarlo a otro sistema

Cómo usar el RAG desde la solución del trabajo, en lugar de abrir su página en una dirección y un puerto.

Hay tres caminos, del más simple al más largo. Los dos primeros no tocan el código del RAG.

| Camino | Qué se hace | Sirve con | Qué se toca |
|---|---|---|---|
| 1. Un enlace | El otro sistema abre la página del RAG | Cualquier sistema | Nada del RAG |
| 2. Un pedido HTTP | El otro sistema manda la pregunta y recibe la respuesta en JSON | Cualquier lenguaje y versión, también .NET Framework 3.5 | Dos líneas de configuración, si el RAG corre en otra máquina |
| 3. Adentro de la solución | Las clases del RAG pasan a ser un proyecto más | Solo una solución en .NET 10 | `Program.cs` y el archivo del proyecto |

## 1. Qué es fijo y qué se puede cambiar

Toda la lógica está en clases que no saben nada de páginas ni de direcciones. `Program.cs` es lo único que las expone: las registra y les pone una dirección a cada una.

| Si querés cambiar | Tocá | No toques |
|---|---|---|
| A qué servidores se conecta | `RagBasico.Api\conexiones.json` (ver [`oficina.md`](oficina.md)) | El código |
| En qué dirección y puerto escucha | `RagBasico.Api\Properties\launchSettings.json`, la línea `applicationUrl` | |
| Desde qué nombres se la puede llamar | `RagBasico.Api\appsettings.json`, la línea `AllowedHosts` | |
| Qué pedidos acepta y cómo se llaman | `RagBasico.Api\Program.cs`, las líneas `app.Map...` | Las clases |
| Cómo se ve | `RagBasico.Api\wwwroot\` | |
| Cómo busca, cómo redacta, cómo carga | `Buscador.cs`, `Redactor.cs`, `Cargador.cs` | `Program.cs` |

## 2. El pedido HTTP

Es el camino que sirve para un sistema en VB.NET sobre .NET Framework.

El otro sistema manda la pregunta a `POST /preguntar`:

```json
{ "pregunta": "¿Qué hay que presentar para pedir el reintegro?" }
```

Y recibe:

```json
{
  "texto": "Hay que presentar el formulario y la factura original [1].",
  "fuentes": [ { "numero": 1, "archivo": "reintegros.pdf", "fragmentoId": 1520 } ],
  "msBusqueda": 180,
  "msModelo": 4200
}
```

`texto` es la respuesta, con las citas entre corchetes. `fuentes` dice de qué archivo salió cada cita. La respuesta trae además cuánto tardó cada parte.

Si el pedido no es válido, la API responde 400. Si falta un servidor, 503. En los dos casos el cuerpo es un JSON con `title` y `detail`, que dicen qué pasó.

Todos los pedidos, con un ejemplo de cada uno, están en `RagBasico.Api\RagBasico.Api.http`.

### Desde VB.NET

Esta función usa solo lo que trae .NET Framework 3.5. El proyecto necesita la referencia `System.Web.Extensions`.

```vb
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web.Script.Serialization

Public Function PreguntarAlRag(ByVal pregunta As String) As String
    Dim serializador As New JavaScriptSerializer()
    Dim pedido As New Dictionary(Of String, Object)
    pedido.Add("pregunta", pregunta)
    Dim cuerpo As Byte() = Encoding.UTF8.GetBytes(serializador.Serialize(pedido))

    Dim http As HttpWebRequest = CType(WebRequest.Create("http://127.0.0.1:5090/preguntar"), HttpWebRequest)
    http.Method = "POST"
    http.ContentType = "application/json; charset=utf-8"
    http.Timeout = 300000
    http.ContentLength = cuerpo.Length
    Using salida As Stream = http.GetRequestStream()
        salida.Write(cuerpo, 0, cuerpo.Length)
    End Using

    Using respuesta As WebResponse = http.GetResponse()
        Using lector As New StreamReader(respuesta.GetResponseStream(), Encoding.UTF8)
            Dim datos As Dictionary(Of String, Object) = serializador.Deserialize(Of Dictionary(Of String, Object))(lector.ReadToEnd())
            Return CStr(datos("texto"))
        End Using
    End Using
End Function
```

Dos cosas a tener en cuenta:

- **La espera.** El modelo tarda varios segundos en redactar, y la primera pregunta después de un rato sin uso tarda más. Por eso el `Timeout` es de 5 minutos, y conviene llamar a la función fuera del hilo de la pantalla.
- **Los errores.** Con un 400 o un 503, `GetResponse` lanza una `WebException`. El motivo está en el cuerpo de `ex.Response`, en el campo `detail`.

### Si el RAG corre en otra máquina

Por defecto la API escucha solo en su propia PC. Si el otro sistema corre en la misma máquina, no hay que tocar nada: la dirección es `http://127.0.0.1:5090`.

Para que la llamen desde otra máquina:

1. En `RagBasico.Api\Properties\launchSettings.json`, cambiar `applicationUrl` a `http://0.0.0.0:5090`.
2. En `RagBasico.Api\appsettings.json`, agregar el nombre de esa máquina a `AllowedHosts`: `127.0.0.1;localhost;SERVIDOR-RAG`.
3. En esa máquina, abrir el puerto 5090 en el firewall, solo para las máquinas que la van a llamar.

**Antes de hacerlo:** la API no tiene usuarios ni permisos. Quien llegue al puerto puede preguntar, y también cargar, borrar y reprocesar documentos. Por eso la regla del firewall tiene que nombrar las máquinas que entran, y no abrir el puerto a toda la red. Si lo va a usar más de un sistema, hay que agregarle usuarios primero.

## 3. Adentro de la solución

Solo sirve si la solución es .NET 10: el proyecto del RAG no compila en .NET Framework. Con una solución en .NET Framework, el camino es el pedido HTTP.

No está hecho. Estos son los lugares que hay que tocar:

1. **Separar las clases de la API.** Hoy todo está en un proyecto web, `RagBasico.Api`. Los archivos `.cs`, menos `Program.cs`, se mueven a un proyecto de biblioteca de clases, con los mismos tres paquetes: Dapper, Microsoft.Data.SqlClient y PdfPig.
2. **Registrar las clases en la otra aplicación.** Son las líneas de `Program.cs` que van desde `AddHttpClient<Ollama>` hasta `AddHostedService<Procesador>`. Se copian tal cual.
3. **Darle la configuración.** Las clases leen cuatro bloques: `Conexiones`, `Rag`, `Ollama` y `Qdrant`. Se copian de `appsettings.json` a la configuración de la otra aplicación.
4. **Llamar a las clases.** Para preguntar, `Redactor.ResponderAsync(pregunta, modo, ct)`, que devuelve lo mismo que `POST /preguntar`. Para cargar, `Documentos.AnotarSubidosAsync(archivos, ct)`. El `Procesador` procesa solo lo que quede pendiente.

Lo que se pierde por este camino son las dos páginas y la franja de estado, que son de `RagBasico.Api`. El chequeo que las alimenta, `Salud.RevisarAsync`, sigue disponible.
