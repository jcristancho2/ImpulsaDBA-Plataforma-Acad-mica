window.impulsaTheme = {
    /**
     * Aplica una lista de colores a variables CSS globales.
     * Espera elementos con propiedades:
     * - colorNombre: nombre lógico (sin los dos guiones), ejemplo: "color-primary"
     * - colorHexadecimal: valor "#RRGGBB"
     */
    applyPalette: function (palette) {
        if (!palette || !Array.isArray(palette)) return;

        const root = document.documentElement;

        palette.forEach(item => {
            if (!item) return;

            const name = (item.colorNombre || item.ColorNombre || "").trim();
            const hex = (item.colorHexadecimal || item.ColorHexadecimal || "").trim();

            if (!name || !hex) return;

            const varName = name.startsWith("--") ? name : `--${name}`;
            root.style.setProperty(varName, hex);
        });
    }
};

