window.pocokShowcase = {
  getCursor(id) {
    const element = document.getElementById(id);
    return element ? element.selectionStart : 0;
  },
  insertCompletion(id, text) {
    const element = document.getElementById(id);
    if (!element) return "";
    const start = element.selectionStart;
    const end = element.selectionEnd;
    const before = element.value.slice(0, start);
    const token = before.match(/[A-Za-z0-9_.:<>]+$/);
    const replaceStart = token ? start - token[0].length : start;
    element.value = element.value.slice(0, replaceStart) + text + element.value.slice(end);
    const cursor = replaceStart + text.length;
    element.focus();
    element.setSelectionRange(cursor, cursor);
    return element.value;
  }
};

document.addEventListener("keydown", event => {
  const element = event.target;
  if (!(element instanceof HTMLTextAreaElement) || element.dataset.showcaseSuggestions !== "open") return;
  const inserts = event.key === "Tab" || (event.key === "Enter" && !event.shiftKey);
  const navigates = event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Escape";
  if (inserts || navigates) event.preventDefault();
}, true);
