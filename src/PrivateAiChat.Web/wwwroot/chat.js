window.privateAiChatComposer = {
  bind: (textareaId, dotNetReference) => {
    const textarea = document.getElementById(textareaId);
    if (!textarea || textarea.dataset.privateAiChatComposerBound === "true") {
      return;
    }

    const resize = () => {
      textarea.style.height = "auto";
      textarea.style.height = `${Math.min(textarea.scrollHeight, 168)}px`;
    };

    textarea.dataset.privateAiChatComposerBound = "true";
    textarea.addEventListener("input", (event) => {
      resize();
      dotNetReference.invokeMethodAsync("UpdateComposerContent", event.target.value);
    });

    textarea.addEventListener("keydown", (event) => {
      if (event.key !== "Enter" || event.shiftKey || event.isComposing) {
        return;
      }

      event.preventDefault();
      dotNetReference.invokeMethodAsync("SendMessageFromKeyboard");
    });

    resize();
  },
  resize: (textareaId) => {
    const textarea = document.getElementById(textareaId);
    if (!textarea) {
      return;
    }

    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(textarea.scrollHeight, 168)}px`;
  },
  scrollToBottom: (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) {
      return;
    }

    element.scrollTo({
      top: element.scrollHeight,
      behavior: "smooth"
    });
  }
};

window.privateAiChatMarkdown = {
  enhanceCodeBlocks: (elementId) => {
    const root = document.getElementById(elementId);
    if (!root) {
      return;
    }

    root.querySelectorAll("pre").forEach((pre) => {
      if (pre.dataset.privateAiChatCodeBlock === "true") {
        return;
      }

      const code = pre.querySelector("code");
      const wrapper = document.createElement("div");
      const button = document.createElement("button");
      const label = getCodeLanguage(code);

      wrapper.className = "markdown-code-block";
      pre.dataset.privateAiChatCodeBlock = "true";

      button.type = "button";
      button.className = "markdown-code-copy";
      button.textContent = label ? `Copy ${label}` : "Copy";
      button.setAttribute("aria-label", "Copy code block");

      button.addEventListener("click", async () => {
        const text = code?.innerText ?? pre.innerText ?? "";

        try {
          await navigator.clipboard.writeText(text);
          button.textContent = "Copied";
          window.setTimeout(() => {
            button.textContent = label ? `Copy ${label}` : "Copy";
          }, 1400);
        } catch {
          button.textContent = "Copy failed";
          window.setTimeout(() => {
            button.textContent = label ? `Copy ${label}` : "Copy";
          }, 1400);
        }
      });

      pre.parentNode.insertBefore(wrapper, pre);
      wrapper.appendChild(button);
      wrapper.appendChild(pre);
    });

    root.querySelectorAll("a[href]").forEach((anchor) => {
      if (!isSafeHref(anchor.getAttribute("href"))) {
        anchor.removeAttribute("href");
        return;
      }

      anchor.setAttribute("target", "_blank");
      anchor.setAttribute("rel", "noopener noreferrer");
    });
  }
};

function getCodeLanguage(code) {
  const className = code?.className ?? "";
  const match = className.match(/language-([a-z0-9+#.-]+)/i);
  return match ? match[1] : "";
}

function isSafeHref(href) {
  if (!href) {
    return false;
  }

  const normalized = href.trim().toLowerCase();
  return normalized.startsWith("http://")
    || normalized.startsWith("https://")
    || normalized.startsWith("mailto:")
    || normalized.startsWith("#")
    || (normalized.startsWith("/") && !normalized.startsWith("//"));
}
