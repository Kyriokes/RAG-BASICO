# RAG-BASICO

Un prototipo de RAG (búsqueda en documentos y respuesta redactada por un modelo de lenguaje), armado por fases con .NET 10, SQL Server 2025 Express, Qdrant y Ollama.

Cada fase agrega una pieza y queda marcada con su branch, así el proyecto se puede recorrer de la versión más chica a la final.

**Esta es la fase 0, el esqueleto:** la configuración del repo, una API vacía que contesta `GET /`, la documentación y los scripts. Todavía no busca ni responde nada.

## Requisitos de esta fase

- SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx (lo fija `global.json`).
- Windows PowerShell 5.1, para los scripts.
- Para probar los pedidos del archivo `.http`: Visual Studio, o VS Code con la extensión "REST Client".

## Cómo correr esta fase

```powershell
cd RagBasico.Api
dotnet run
```

Abrí `http://127.0.0.1:5080/` en el navegador, o mandá el pedido de `RagBasico.Api\RagBasico.Api.http`. Tiene que devolver el nombre del proyecto, la fase y la versión de .NET.

Para controlar que la documentación coincida con el código, desde la raíz del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1
```

## Fases

| Fase | Branch | Qué agrega | Puerto | Base | Número principal | Detalle |
|---|---|---|---|---|---|---|
| 0 | `fase-0-base` | El esqueleto: configuración del repo, API vacía, documentación y scripts | 5080 | no usa | ninguno | [fase-0-base.md](docs/fases/fase-0-base.md) |

Para ver qué agregó una fase: `git diff fase-0-base fase-1-ollama`, o la página "compare" de GitHub entre las dos branches. En un clon nuevo, primero hay que traer las branches de fase: está en [docs/worktrees.md](docs/worktrees.md).

## Cómo recorrer las fases

Cada fase se puede abrir en su propia carpeta, sin cambiar de branch. Desde la raíz del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1
```

El paso a paso, también para un clon nuevo, está en [docs/worktrees.md](docs/worktrees.md).

## Documentación

- [docs/arquitectura.md](docs/arquitectura.md): las piezas, los recorridos, las tablas y la configuración de esta versión.
- [docs/responsabilidades.md](docs/responsabilidades.md): qué hace cada archivo, clase, función y endpoint.
- [docs/worktrees.md](docs/worktrees.md): cómo abrir y correr cada fase en su carpeta.
- [docs/fases/](docs/fases/): un documento por fase, con lo que cambió, el orden de lectura y los resultados.

## Erratas

Las erratas de esta versión están en `docs/erratas.md` de la branch de fase más nueva.
