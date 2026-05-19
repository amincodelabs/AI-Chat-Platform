window.privateAiChatComposer = {
  bind: (textareaId, dotNetReference) => {
    const textarea = document.getElementById(textareaId);
    if (!textarea || textarea.dataset.privateAiChatComposerBound === "true") {
      return;
    }

    textarea.dataset.privateAiChatComposerBound = "true";
    textarea.addEventListener("input", (event) => {
      dotNetReference.invokeMethodAsync("UpdateComposerContent", event.target.value);
    });

    textarea.addEventListener("keydown", (event) => {
      if (event.key !== "Enter" || event.shiftKey || event.isComposing) {
        return;
      }

      event.preventDefault();
      dotNetReference.invokeMethodAsync("SendMessageFromKeyboard");
    });
  }
};
