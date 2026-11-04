// Interop de CineList: localStorage y tema
window.cinelist = {
    leer: (clave) => { try { return localStorage.getItem(clave); } catch { return null; } },
    guardar: (clave, valor) => { try { localStorage.setItem(clave, valor); } catch { } },
    temaActual: () => document.documentElement.getAttribute('data-tema') || 'oscuro',
    fijarTema: (tema) => document.documentElement.setAttribute('data-tema', tema),
};
