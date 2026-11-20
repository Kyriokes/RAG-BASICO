// La franja de estado de las dos páginas: pide GET /salud y dibuja un punto por chequeo.
// Verde: anda. Amarillo: anda a medias. Rojo: no anda. Debajo, el detalle de los que no están en verde, que dice qué hacer.
// Todo queda adentro de mostrarEstado: así sus nombres no chocan con los de la página que la usa.

function mostrarEstado(elemento) {
  async function pedirSalud() {
    let informe = null;
    try {
      // /salud responde 503 cuando hay un error, pero igual trae el informe en JSON.
      const pedido = await fetch('/salud');
      informe = await pedido.json();
    } catch (e) {
      informe = null;
    }
    dibujarSalud(informe);
  }

  function dibujarSalud(informe) {
    elemento.replaceChildren();
    if (!informe || !informe.chequeos) {
      const texto = document.createElement('p');
      texto.className = 'error';
      texto.textContent = 'No se pudo hablar con la API. ¿Está corriendo? Se arranca con iniciar.cmd.';
      elemento.appendChild(texto);
      return;
    }

    const lista = document.createElement('ul');
    for (const chequeo of informe.chequeos) {
      const item = document.createElement('li');
      const punto = document.createElement('span');
      punto.className = 'punto ' + chequeo.estado;
      item.appendChild(punto);
      // textContent y no innerHTML: lo que viene de la API nunca se interpreta como HTML.
      item.appendChild(document.createTextNode(chequeo.nombre));
      item.title = chequeo.detalle;
      lista.appendChild(item);
    }
    elemento.appendChild(lista);

    for (const chequeo of informe.chequeos) {
      if (chequeo.estado !== 'ok') {
        const texto = document.createElement('p');
        texto.className = chequeo.estado;
        texto.textContent = chequeo.nombre + ': ' + chequeo.detalle;
        elemento.appendChild(texto);
      }
    }
  }

  pedirSalud();
  setInterval(pedirSalud, 15000);
}
