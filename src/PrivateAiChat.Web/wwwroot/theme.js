window.privateAiChatTheme = {
  get: (key) => localStorage.getItem(key),
  set: (key, theme) => {
    const selectedTheme = theme === "light" || theme === "dark" ? theme : "system";
    const resolvedTheme = selectedTheme === "system"
      ? (window.matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark")
      : selectedTheme;

    document.documentElement.dataset.theme = resolvedTheme;
    document.documentElement.dataset.themePreference = selectedTheme;
    localStorage.setItem(key, selectedTheme);
  },
  boot: (key) => {
    window.privateAiChatTheme.set(key, localStorage.getItem(key) || "system");
  }
};

window.privateAiChatTheme.boot("private-ai-chat-theme");

window
  .matchMedia("(prefers-color-scheme: light)")
  .addEventListener("change", () => {
    const key = "private-ai-chat-theme";
    if ((localStorage.getItem(key) || "system") === "system") {
      window.privateAiChatTheme.set(key, "system");
    }
  });
