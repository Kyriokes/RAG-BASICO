# Una carpeta por fase

Cada fase del proyecto tiene su branch. Para leer o correr una fase vieja no hace falta cambiar de branch: cada una se puede abrir en su propia carpeta, con git worktree.

## Qué es un worktree

Git puede tener varias carpetas de trabajo del mismo repo, cada una parada en una versión distinta, sin copiar la historia. Cada una de esas carpetas es un worktree. La carpeta principal del repo es el primero; las de las fases son otros.

Las carpetas de fase quedan "separadas" (detached HEAD): muestran la versión de su branch, pero no están paradas sobre ella. Sirven para leer y correr, no para commitear. Una configuración local de git, que no es parte del repo, frena los commits en esas carpetas.

## Dónde quedan

```text
..\RAG-BASICO\                       la carpeta principal: acá se trabaja
..\RAG-BASICO-fases\fase-0-base\     una carpeta por fase
..\RAG-BASICO-fases\fase-1-ollama\
..\RAG-BASICO-fases\...
```

La carpeta de las fases va al lado del repo, con el mismo nombre y `-fases` al final.

## Crear o actualizar todas las carpetas

Desde la carpeta principal del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1
```

El script crea la carpeta de cada fase que todavía no la tiene, y mueve las existentes a la versión actual de su branch. Si una carpeta tiene cambios en archivos que git sigue, avisa y no la toca; los archivos nuevos que no sigue git, como la configuración que deja un editor, no lo frenan y se conservan. Hay que correrlo después de cerrar cada fase y después de cualquier arreglo a una fase vieja.

`-ExecutionPolicy Bypass` hace falta porque Windows, por defecto, no deja correr scripts `.ps1` propios. Vale solo para ese comando.

## En un clon nuevo

En un clon nuevo, por ejemplo en otra PC, las fases están solo en el remoto (`origin/fase-0-base`, `origin/fase-1-ollama`...). El script las toma de ahí, así que alcanza con correrlo igual que siempre.

Para comparar fases con `git diff fase-0-base fase-1-ollama`, o para trabajar sobre ellas, hacen falta como branches locales. Parado en `main`:

```powershell
git switch main
git fetch origin "refs/heads/fase-*:refs/heads/fase-*"
```

Si se lo corre parado en una branch `fase-*`, git se niega. Si una fase vieja se reacomodó con un push forzado, se trae con `+` adelante: `"+refs/heads/fase-*:refs/heads/fase-*"`, que pisa las branches `fase-*` locales de ese clon.

## Hacerlo a mano

| Para | Comando, desde la carpeta principal |
|---|---|
| Crear la carpeta de una fase | `git worktree add --detach ..\RAG-BASICO-fases\fase-0-base fase-0-base` |
| Lo mismo en un clon nuevo, desde el remoto | `git worktree add --detach ..\RAG-BASICO-fases\fase-0-base origin/fase-0-base` |
| Moverla a la versión actual de su branch | `git -C ..\RAG-BASICO-fases\fase-0-base switch --detach fase-0-base` |
| Ver las carpetas que hay | `git worktree list` |
| Borrar una carpeta | `git worktree remove ..\RAG-BASICO-fases\fase-0-base` |
| Limpiar el registro de carpetas borradas a mano | `git worktree prune` |

## Correr una fase

Desde la carpeta principal:

```powershell
cd ..\RAG-BASICO-fases\fase-0-base\RagBasico.Api
dotnet run
```

O abrir `RagBasico.slnx` de esa carpeta en Visual Studio y apretar F5. Cada fase trae su propio `README.md` con los pasos exactos de esa versión.

## La primera vez en cada carpeta

Desde la fase 2, cada fase usa su propia base de datos, que hay que preparar una vez:

1. `scripts\crear-base.ps1`, desde la fase 2.
2. `POST /indexar`, desde la fase 2.
3. `POST /vectores`, desde la fase 5.
4. `POST /qdrant/reconstruir`, desde la fase 7.

## Correr dos fases a la vez

Cada fase usa su propio puerto y, desde la fase 2, su propia base. Por eso dos fases pueden estar corriendo al mismo tiempo, por ejemplo para comparar sus respuestas.

| Fase | Puerto | Base de datos |
|---|---|---|
| `fase-N-...` | `508N` | `RagBasico_FN`, desde la fase 2 |
| `fase-0-base` | 5080 | no usa |

Desde la fase 7, cada fase usa también su propia colección de Qdrant: `fragmentos_fN`.

## Borrar una carpeta

1. Cerrá Visual Studio o VS Code en esa carpeta, y frená la API si está corriendo. Si no, Windows deja archivos tomados y el borrado queda a medias.
2. `git worktree remove ..\RAG-BASICO-fases\<fase>`.
3. Si falla, o si la borraste desde el Explorador, borrá lo que quedó y corré `git worktree prune`.

## Qué no hacer

- **Commitear en una carpeta de fase.** Los cambios se hacen siempre en la carpeta principal.
- **Borrar una carpeta desde el Explorador sin correr después `git worktree prune`.** Git la sigue teniendo registrada y no deja crearla de nuevo.
- **Usar `git clean -x`.** Borra también todo lo que git ignora, no solo `bin` y `obj`.
