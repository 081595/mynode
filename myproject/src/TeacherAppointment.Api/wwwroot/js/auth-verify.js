(() => {
    const state = { sessionId: null };

    function byId(id) {
        return document.getElementById(id);
    }

    async function handleAsyncEvent(event) {
        const detail = event.detail || {};
        const form = detail.form;
        const payload = detail.payload || {};
        if (!(form instanceof HTMLFormElement) || !payload.success) {
            return;
        }

        if (form.id === "create-qr-form" && payload.sessionId) {
            state.sessionId = payload.sessionId;
            const image = byId("qr-image");
            if (image instanceof HTMLImageElement) {
                image.src = payload.qrCodeDataUri;
                image.classList.remove("d-none");
            }
            await connectSignalR(payload.sessionId);
        }

        if (form.id === "exchange-form" && payload.redirectUrl) {
            window.location.href = payload.redirectUrl;
        }
    }

    async function connectSignalR(sessionId) {
        if (!window.signalR || !sessionId) {
            revealSignalRFallback();
            return;
        }

        const connection = new window.signalR.HubConnectionBuilder()
            .withUrl("/hubs/auth-challenge")
            .withAutomaticReconnect()
            .build();

        connection.on("desktop.redirect", async (payload) => {
            if (!payload || payload.sessionId !== sessionId) {
                return;
            }

            const exchangeForm = byId("exchange-form");
            if (!(exchangeForm instanceof HTMLFormElement)) {
                return;
            }

            const response = await fetch(exchangeForm.action, {
                method: "POST",
                body: new FormData(exchangeForm),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const exchange = await response.json();
            if (exchange.success && exchange.redirectUrl) {
                window.location.href = exchange.redirectUrl;
            }
        });

        try {
            await connection.start();
            await connection.invoke("JoinDesktopSessionAsync", sessionId);
        } catch {
            revealSignalRFallback();
        }
    }

    function revealSignalRFallback() {
        const warning = byId("signalr-warning");
        if (warning) {
            warning.classList.remove("d-none");
        }
    }

    document.addEventListener("DOMContentLoaded", () => {
        document.addEventListener("portal:async:done", handleAsyncEvent);
    });
})();
