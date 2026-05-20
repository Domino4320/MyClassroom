(function () {
    const IS_AUTH = document.body.dataset.forumAuth === "true";
    const parentMessageInput = document.getElementById("parentMessageId");
    const parentMessageFormInput = document.getElementById("parentMessageIdForm");
    const replyBanner = document.getElementById("replyBanner");
    const replyToName = document.getElementById("replyToName");
    const cancelReplyBtn = document.getElementById("cancelReplyBtn");
    const textBox = document.getElementById("textBox");

    function setReply(messageId, name) {
        if (!IS_AUTH || !parentMessageInput || !parentMessageFormInput || !replyBanner || !replyToName) return;
        parentMessageInput.value = String(messageId);
        parentMessageFormInput.value = String(messageId);
        replyToName.textContent = name || "";
        replyBanner.style.display = "block";
        if (textBox) textBox.focus();
    }

    function clearReply() {
        if (!parentMessageInput || !parentMessageFormInput || !replyBanner || !replyToName) return;
        parentMessageInput.value = "";
        parentMessageFormInput.value = "";
        replyToName.textContent = "";
        replyBanner.style.display = "none";
    }

    if (!IS_AUTH) return;

    document.addEventListener("click", (event) => {
        const btn = event.target.closest(".js-reply-btn");
        if (!btn) return;
        const messageId = btn.getAttribute("data-message-id");
        const messageUser = btn.getAttribute("data-message-user") || "";
        if (!messageId) return;
        setReply(messageId, messageUser);
    });

    if (cancelReplyBtn) {
        cancelReplyBtn.addEventListener("click", clearReply);
    }
})();
