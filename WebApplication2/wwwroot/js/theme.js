(function () {
    const root = document.documentElement;
    const STORAGE_KEY = "theme";
    const THEME_LIGHT = "light";
    const THEME_DARK = "dark";

    function getSavedTheme() {
        try {
            const t = localStorage.getItem(STORAGE_KEY);
            return t === THEME_LIGHT || t === THEME_DARK ? t : null;
        } catch (e) {
            return null;
        }
    }

    function getPreferredTheme() {
        try {
            return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
                ? THEME_DARK
                : THEME_LIGHT;
        } catch (e) {
            return THEME_LIGHT;
        }
    }

    function updateToggleLabels() {
        const current = root.dataset.theme || THEME_LIGHT;
        document.querySelectorAll("[data-theme-toggle]").forEach((btn) => {
            const label = btn.querySelector("[data-theme-toggle-label]");
            if (!label) return;
            label.textContent = current === THEME_DARK ? "Светлая" : "Тёмная";
        });
    }

    function setTheme(theme) {
        root.dataset.theme = theme;
        updateToggleLabels();
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (e) {
            // ignore
        }
    }

    // Initial theme (picked once on load)
    const saved = getSavedTheme();
    const initialTheme = saved || root.dataset.theme || getPreferredTheme();
    if (!root.dataset.theme) root.dataset.theme = initialTheme;
    updateToggleLabels();

    // Single delegated handler for all toggle buttons
    document.addEventListener("click", (e) => {
        const btn = e.target && e.target.closest ? e.target.closest("[data-theme-toggle]") : null;
        if (!btn) return;

        const next = (root.dataset.theme || THEME_LIGHT) === THEME_DARK ? THEME_LIGHT : THEME_DARK;
        setTheme(next);
    });
})();

