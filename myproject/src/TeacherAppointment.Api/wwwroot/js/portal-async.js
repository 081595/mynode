(() => {
    async function submitAsyncForm(form) {
        form.classList.add("async-busy");
        const formData = new FormData(form);
        const statusTarget = form.dataset.statusTarget ? document.querySelector(form.dataset.statusTarget) : null;

        try {
            const response = await fetch(form.action, {
                method: form.method || "POST",
                body: formData,
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const payload = await response.json();
            if (statusTarget) {
                statusTarget.innerHTML = "";
                const klass = payload.success ? "alert-success" : "alert-danger";
                statusTarget.innerHTML = `<div class=\"alert ${klass}\" role=\"status\">${payload.message}</div>`;
            }

            document.dispatchEvent(new CustomEvent("portal:async:done", {
                detail: {
                    form,
                    payload
                }
            }));

            if (payload.success && payload.reload) {
                window.location.reload();
            }
        } catch {
            if (statusTarget) {
                statusTarget.innerHTML = '<div class="alert alert-warning" role="status">操作未完成，請稍後再試。</div>';
            }
        } finally {
            form.classList.remove("async-busy");
        }
    }

    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (!form.matches("[data-async-form='true']")) {
            return;
        }

        event.preventDefault();
        submitAsyncForm(form);
    });
})();
