// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

(() => {
    const themeCookie = "pocok.showcase.theme";
    const legacyThemeStorageKey = "pocok.showcase.theme";
    const featureCookiePrefix = "pocok.showcase.feature.";
    const supportedFeatures = new Set(["log-console"]);
    const cookieMaxAgeSeconds = 31_536_000;
    const root = document.documentElement;
    const normalizeTheme = value => value === "light" || value === "dark" ? value : null;
    const normalizeBoolean = value => value === "true" ? true : value === "false" ? false : null;
    const preferred = () => window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark";

    const readCookie = name => {
        const prefix = `${encodeURIComponent(name)}=`;
        try {
            const entry = document.cookie
                .split(";")
                .map(value => value.trim())
                .find(value => value.startsWith(prefix));
            return entry ? decodeURIComponent(entry.slice(prefix.length)) : null;
        } catch {
            return null;
        }
    };

    const writeCookie = (name, value) => {
        const secure = window.location.protocol === "https:" ? "; Secure" : "";
        try {
            document.cookie = `${encodeURIComponent(name)}=${encodeURIComponent(value)}; Max-Age=${cookieMaxAgeSeconds}; Path=/; SameSite=Lax${secure}`;
            return readCookie(name) === value;
        } catch {
            return false;
        }
    };

    const readLegacyTheme = () => {
        try {
            return normalizeTheme(window.localStorage.getItem(legacyThemeStorageKey));
        } catch {
            return null;
        }
    };

    const persistTheme = theme => {
        if (writeCookie(themeCookie, theme)) {
            try {
                window.localStorage.removeItem(legacyThemeStorageKey);
            } catch {
                // The cookie is authoritative even when legacy storage cannot be cleared.
            }
            return;
        }

        try {
            window.localStorage.setItem(legacyThemeStorageKey, theme);
        } catch {
            // The applied in-memory choice remains usable when persistence is unavailable.
        }
    };

    const readTheme = () => {
        const selected = normalizeTheme(readCookie(themeCookie));
        if (selected) return selected;

        const legacy = readLegacyTheme();
        if (legacy) persistTheme(legacy);
        return legacy;
    };

    const apply = theme => {
        const selected = normalizeTheme(theme) ?? preferred();
        root.dataset.theme = selected;
        root.style.colorScheme = selected;
        return selected;
    };

    const featureCookie = name => {
        if (!supportedFeatures.has(name)) throw new RangeError(`Unsupported Showcase feature: ${name}`);
        return `${featureCookiePrefix}${name}`;
    };

    const applyFeature = (name, enabled) => {
        if (name === "log-console") root.dataset.logConsole = enabled ? "visible" : "hidden";
        return enabled;
    };

    const readFeature = (name, defaultValue) => {
        const stored = normalizeBoolean(readCookie(featureCookie(name)));
        return applyFeature(name, stored ?? defaultValue === true);
    };

    const persistFeature = (name, enabled) => {
        const selected = applyFeature(name, enabled === true);
        writeCookie(featureCookie(name), selected.toString());
        return selected;
    };

    apply(readTheme());
    readFeature("log-console", true);

    window.pocokShowcaseTheme = {
        current: () => apply(root.dataset.theme),
        toggle: () => {
            const selected = apply(root.dataset.theme === "dark" ? "light" : "dark");
            persistTheme(selected);
            return selected;
        }
    };

    window.pocokShowcasePreferences = {
        feature: (name, defaultValue) => readFeature(name, defaultValue),
        setFeature: (name, enabled) => persistFeature(name, enabled)
    };
})();
