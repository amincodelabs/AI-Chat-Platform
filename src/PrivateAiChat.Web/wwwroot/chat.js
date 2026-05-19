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
