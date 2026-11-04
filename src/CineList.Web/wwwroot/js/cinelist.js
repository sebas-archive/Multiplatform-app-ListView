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

    window.cinelist = {
        leer: (clave) => { try { return localStorage.getItem(clave); } catch { return null; } },
        guardar: (clave, valor) => { try { localStorage.setItem(clave, valor); } catch { } },
        temaActual: () => document.documentElement.getAttribute('data-tema') || 'oscuro',
        fijarTema: (tema) => document.documentElement.setAttribute('data-tema', tema),
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

        // Ráfaga de puntos color acento desde el centro de un elemento; arco en grados (0 = derecha, 90 = abajo).
        // Los puntos van en <body> (position: fixed) para que la fila no los recorte y se quitan al terminar.
        rafaga: (el, puntos, arcoMin, arcoMax) => {
            if (!el || reducir() || !el.animate) return;
            const r = el.getBoundingClientRect();
            const cx = r.left + r.width / 2, cy = r.top + r.height / 2;
            const color = getComputedStyle(document.documentElement).getPropertyValue('--acento').trim() || '#F5C518';
            const paso = (arcoMax - arcoMin) / puntos;
            for (let i = 0; i < puntos; i++) {
                const angulo = (arcoMin + (i + 0.5) * paso + (Math.random() - 0.5) * paso * 0.4) * Math.PI / 180;
                const distancia = 40 + Math.random() * 20;
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
    };
})();
