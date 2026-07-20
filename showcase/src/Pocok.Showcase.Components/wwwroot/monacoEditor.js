// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 Pocok contributors

(() => {
  const providers = new Map();
  const instances = new Set();
  let themeObserver = null;

  const currentTheme = () =>
    document.documentElement.dataset.theme === "light" ? "vs" : "vs-dark";

  const applyTheme = () => {
    if (window.monaco?.editor) window.monaco.editor.setTheme(currentTheme());
  };

  const ensureThemeObserver = () => {
    if (themeObserver) return;
    themeObserver = new MutationObserver(applyTheme);
    themeObserver.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["data-theme"]
    });
  };

  window.pocokMonaco = {
    isReady: () => Boolean(window.monaco?.editor && window.monaco?.languages),

    configure: (id, language, completions) => {
      if (!window.monaco?.languages) return false;
      providers.get(id)?.dispose();
      providers.delete(id);
      instances.add(id);

      const safeItems = Array.isArray(completions) ? completions : [];
      if (safeItems.length > 0) {
        const provider = window.monaco.languages.registerCompletionItemProvider(language, {
          provideCompletionItems: (model, position) => {
            const word = model.getWordUntilPosition(position);
            const range = {
              startLineNumber: position.lineNumber,
              endLineNumber: position.lineNumber,
              startColumn: word.startColumn,
              endColumn: word.endColumn
            };
            return {
              suggestions: safeItems.map(item => ({
                label: String(item.label ?? ""),
                insertText: String(item.insertText ?? ""),
                documentation: String(item.documentation ?? ""),
                kind: window.monaco.languages.CompletionItemKind.Snippet,
                insertTextRules:
                  window.monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                range
              }))
            };
          }
        });
        providers.set(id, provider);
      }

      ensureThemeObserver();
      applyTheme();
      return true;
    },

    dispose: id => {
      providers.get(id)?.dispose();
      providers.delete(id);
      instances.delete(id);
      if (instances.size === 0 && themeObserver) {
        themeObserver.disconnect();
        themeObserver = null;
      }
    }
  };
})();
