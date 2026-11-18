window.vaghtNegarApi = {
    get: async function (path) {
        const response = await fetch("/" + path.replace(/^\/+/, ""), {
            method: "GET",
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        });
        return { status: response.status, body: await response.text() };
    },
    request: async function (method, path, jsonBody) {
        const response = await fetch("/" + path.replace(/^\/+/, ""), {
            method: method,
            credentials: "same-origin",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json"
            },
            body: jsonBody
        });
        return { status: response.status, body: await response.text() };
    }
};
