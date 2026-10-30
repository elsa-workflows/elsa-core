(() => {
    const base64UrlEncode = bytes => {
        let binary = '';
        for (const byte of bytes)
            binary += String.fromCharCode(byte);

        return btoa(binary)
            .replaceAll('+', '-')
            .replaceAll('/', '_')
            .replace(/=+$/u, '');
    };

    const randomValue = () => {
        const bytes = new Uint8Array(32);
        crypto.getRandomValues(bytes);
        return base64UrlEncode(bytes);
    };

    const createPkce = async () => {
        const state = randomValue();
        const codeVerifier = randomValue();
        const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(codeVerifier));

        return {
            state,
            codeVerifier,
            codeChallenge: base64UrlEncode(new Uint8Array(digest))
        };
    };

    window.elsaExternalAuthentication = Object.freeze({ createPkce });
})();
