window.vaghtNegarApi = {
    get: async function (path) {
        const response = await fetch("/" + path.replace(/^\/+/, ""), {
            method: "GET",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        });
        return { status: response.status, body: await response.text() };
    }
};
