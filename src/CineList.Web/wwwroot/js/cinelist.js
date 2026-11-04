// Interop de CineList: localStorage, tema y animaciones puntuales
(function () {
    const reducir = () => window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Anima con Web Animations y resuelve al terminar (o enseguida si no hay elemento / movimiento reducido)
    const animar = (el, cuadros, opciones) => {
        if (!el || reducir() || !el.animate) return Promise.resolve();
        const a = el.animate(cuadros, opciones);
        // Respaldo: en una pestaña en segundo plano "finished" puede tardar; no se bloquea el cambio de texto
        const respaldo = new Promise(r => setTimeout(r, opciones.duration + 80));
        return Promise.race([a.finished.then(() => { }, () => { }), respaldo]);
    };

    // Espera (máx. ms) a que exista un elemento; con MutationObserver porque durante la captura de una
    // View Transition el renderizado está suspendido y requestAnimationFrame no corre
    const esperarElemento = (selector, ms) => new Promise(resolver => {
        const ya = document.querySelector(selector);
        if (ya) { resolver(ya); return; }
        let hecho = false;
        const fin = (el) => { if (hecho) return; hecho = true; obs.disconnect(); clearTimeout(tope); resolver(el); };
        const obs = new MutationObserver(() => { const el = document.querySelector(selector); if (el) fin(el); });
        obs.observe(document.body, { childList: true, subtree: true });
        const tope = setTimeout(() => fin(document.querySelector(selector)), ms);
    });

    let enTransicion = false;
    let scrollLista = 0;
    let parallax = null;

    // Transición del póster con View Transitions: el póster de origen recibe el nombre solo para la
    // captura inicial; tras navegar, lo recibe el de destino. Así el nombre nunca está dos veces en el DOM.
    // Devuelve 'no' si no se puede (sin soporte o movimiento reducido): quien llama navega normal;
    // 'ignorada' si ya hay otra transición en curso (no se hizo nada); 'ok' si ya se encargó de navegar.
    // Con `atras` navega con history.back() en vez de abrir la url (así el historial no rebota).
    const transicionPoster = (origen, url, selectorDestino, nombre, alLlegar, atras) => {
        if (!document.startViewTransition || reducir() || !window.Blazor || !Blazor.navigateTo || !origen) return 'no';
        if (enTransicion) return 'ignorada'; // doble clic durante la transición: se ignora
        enTransicion = true;
        const vtn = 'poster-' + nombre;
        let destino = null;
        origen.style.viewTransitionName = vtn;
        document.documentElement.classList.add('vt-poster');
        // Se navega una sola vez: desde la captura o, si esta no llega (pestaña sin pintar), desde el respaldo
        let navegado = false;
        const navegar = () => {
            if (navegado) return false;
            navegado = true;
            origen.style.viewTransitionName = '';
            if (atras) history.back(); else Blazor.navigateTo(url);
            return true;
        };
        let limpio = false;
        const limpiar = () => {
            if (limpio) return; // una sola vez (respaldo o fin de la transición)
            limpio = true;
            origen.style.viewTransitionName = '';
            if (destino) destino.style.viewTransitionName = '';
            document.documentElement.classList.remove('vt-poster');
            enTransicion = false;
        };
        let t;
        try {
            t = document.startViewTransition(async () => {
                if (!navegar()) return;
                destino = await esperarElemento(selectorDestino, 800);
                if (alLlegar) alLlegar();
                if (destino) destino.style.viewTransitionName = vtn;
            });
        } catch {
            limpiar();
            return 'no';
        }
        // Respaldo: si la captura no empieza en 400 ms (p. ej. la pestaña no está pintando) se salta la
        // transición y se navega igual; el clic nunca se pierde
        setTimeout(() => {
            if (navegado) return;
            try { t.skipTransition(); } catch { }
            navegar();
            limpiar();
        }, 400);
        t.finished.catch(() => { }).finally(limpiar);
        return 'ok';
    };

    window.cinelist = {
        leer: (clave) => { try { return localStorage.getItem(clave); } catch { return null; } },
        guardar: (clave, valor) => { try { localStorage.setItem(clave, valor); } catch { } },
        temaActual: () => document.documentElement.getAttribute('data-tema') || 'oscuro',
        fijarTema: (tema) => document.documentElement.setAttribute('data-tema', tema),
        // Panel de filtros: bloquea el scroll del fondo, mueve el foco a la hoja y atrapa Tab dentro
        abrirPanel: (hoja) => {
            const previo = document.activeElement;
            window.cinelist._previo = previo;
            document.documentElement.classList.add('sin-scroll');
            if (!hoja) return;
            hoja.focus({ preventScroll: true });
            hoja._trampa = (e) => {
                if (e.key !== 'Tab') return;
                const f = [...hoja.querySelectorAll('button:not([disabled]),[href],input,[tabindex]:not([tabindex="-1"])')].filter(x => x.offsetParent !== null);
                if (!f.length) return;
                const primero = f[0], ultimo = f[f.length - 1];
                if (e.shiftKey && (document.activeElement === primero || document.activeElement === hoja)) { e.preventDefault(); ultimo.focus(); }
                else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
            };
            hoja.addEventListener('keydown', hoja._trampa);
        },
        cerrarPanel: () => document.documentElement.classList.remove('sin-scroll'),
        // Reduce una imagen subida: decodifica respetando la orientación EXIF, escala (lado mayor = ladoMax,
        // sin agrandar) y exporta JPEG. Devuelve el base64 sin prefijo, o null si el archivo no es una imagen.
        reducirImagen: async (bytes, ladoMax, calidad) => {
            let mapa;
            try {
                mapa = await createImageBitmap(new Blob([bytes]), { imageOrientation: 'from-image' });
            } catch { return null; }
            try {
                const mayor = Math.max(mapa.width, mapa.height);
                const factor = mayor > ladoMax ? ladoMax / mayor : 1;
                const ancho = Math.max(1, Math.round(mapa.width * factor));
                const alto = Math.max(1, Math.round(mapa.height * factor));
                const lienzo = document.createElement('canvas');
                lienzo.width = ancho;
                lienzo.height = alto;
                const ctx = lienzo.getContext('2d');
                ctx.fillStyle = '#ffffff'; // PNG con transparencia: fondo blanco en vez de negro
                ctx.fillRect(0, 0, ancho, alto);
                ctx.drawImage(mapa, 0, 0, ancho, alto);
                const url = lienzo.toDataURL('image/jpeg', calidad);
                return url.substring(url.indexOf(',') + 1);
            } finally { mapa.close(); }
        },
        enLinea: () => navigator.onLine !== false,
        reducirMovimiento: reducir,

        // Giro de la etiqueta VISTA/PENDIENTE en dos mitades: 0 -> 90 (se cambia el texto) -> 0
        girarIda: (el) => animar(el, [{ transform: 'perspective(300px) rotateY(0deg)' }, { transform: 'perspective(300px) rotateY(90deg)' }],
            { duration: 150, easing: 'cubic-bezier(.55,0,1,.45)', fill: 'forwards' }),
        girarVuelta: (el) => {
            // Quita la ida (y su "fill: forwards") para que la etiqueta no quede girada
            if (el && el.getAnimations) el.getAnimations().forEach(a => a.cancel());
            return animar(el, [{ transform: 'perspective(300px) rotateY(90deg)' }, { transform: 'perspective(300px) rotateY(0deg)' }],
                { duration: 150, easing: 'cubic-bezier(0,.55,.45,1)' });
        },

        // Ráfaga de puntos color acento desde un elemento; arco en grados (0 = derecha, 90 = abajo, -90 = arriba).
        // origen: 'centro' o 'arriba' (centro del borde superior). Los puntos van en <body> (position: fixed)
        // para que nada los recorte y se quitan al terminar.
        rafaga: (el, puntos, arcoMin, arcoMax, distanciaMin = 40, distanciaMax = 60, origen = 'centro') => {
            if (!el || reducir() || !el.animate) return;
            const r = el.getBoundingClientRect();
            const cx = r.left + r.width / 2, cy = origen === 'arriba' ? r.top : r.top + r.height / 2;
            const color = getComputedStyle(document.documentElement).getPropertyValue('--acento').trim() || '#F5C518';
            const paso = (arcoMax - arcoMin) / puntos;
            for (let i = 0; i < puntos; i++) {
                const angulo = (arcoMin + (i + 0.5) * paso + (Math.random() - 0.5) * paso * 0.4) * Math.PI / 180;
                const distancia = distanciaMin + Math.random() * (distanciaMax - distanciaMin);
                const tam = 6 + Math.floor(Math.random() * 3);
                const p = document.createElement('span');
                p.className = 'particula';
                p.setAttribute('aria-hidden', 'true');
                Object.assign(p.style, {
                    left: (cx - tam / 2) + 'px', top: (cy - tam / 2) + 'px',
                    width: tam + 'px', height: tam + 'px', background: color,
                });
                document.body.appendChild(p);
                const dx = Math.cos(angulo) * distancia, dy = Math.sin(angulo) * distancia;
                const a = p.animate([
                    { transform: 'translate(0,0) scale(1)', opacity: 1 },
                    { transform: `translate(${dx}px,${dy}px) scale(0)`, opacity: 0 },
                ], { duration: 420, easing: 'cubic-bezier(.22,.61,.36,1)' });
                a.finished.finally(() => p.remove());
                setTimeout(() => p.remove(), 1000); // por si la animación se cancela
            }
        },

        // Lista -> detalle: el póster de la fila vuela al del detalle. fila: el elemento de la fila.
        navegarConTransicion: (fila, url, nombre) => {
            const poster = fila && fila.querySelector('.poster');
            const y = window.scrollY;
            const ok = transicionPoster(poster, url, `[data-vt-detalle="${nombre}"]`, nombre);
            scrollLista = y; // para volver a la misma altura con el botón Volver
            return ok;
        },

        // Detalle -> lista (botón Volver): el póster regresa a su fila si está visible en la lista
        volverConTransicion: (marco, url, nombre, atras) =>
            transicionPoster(marco, url, `[data-vt-fila="${nombre}"] .poster`, nombre, () => window.scrollTo(0, scrollLista), atras),

        // Parallax del detalle: el póster baja un poco (máx. 16 px) y se atenúa; el fondo va más lento
        // que el contenido. Un solo listener pasivo, a lo sumo una actualización por cuadro.
        iniciarParallax: (poster, fondo) => {
            window.cinelist.detenerParallax();
            if (!poster || reducir()) return;
            let pendiente = false;
            const aplicar = () => {
                pendiente = false;
                const y = Math.max(0, window.scrollY); // el rebote no mueve nada
                poster.style.transform = `translateY(${Math.min(y * 0.5, 16)}px)`;
                poster.style.opacity = String(1 - Math.min(y / 220, 1) * 0.7);
                if (fondo) fondo.style.transform = `translateY(${y * 0.3}px)`;
            };
            const alDesplazar = () => {
                if (pendiente) return;
                pendiente = true;
                requestAnimationFrame(aplicar);
            };
            window.addEventListener('scroll', alDesplazar, { passive: true });
            parallax = alDesplazar;
            aplicar();
        },
        detenerParallax: () => {
            if (parallax) window.removeEventListener('scroll', parallax);
            parallax = null;
        },
    };
})();
