window.privateAiChatTheme = {
  get: (key) => localStorage.getItem(key),
  set: (key, theme) => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem(key, theme);
  },
  boot: (key) => {
    const theme = localStorage.getItem(key) || "dark";
    document.documentElement.dataset.theme = theme;
  }
};

window.privateAiChatTheme.boot("private-ai-chat-theme");
