// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function ensureBackslash(url: string): string {
    if (!url.endsWith('/'))
        return url + '/';
    return url;
}

export const BackendServiceUrl =
    process.env.REACT_APP_BACKEND_URI == null || process.env.REACT_APP_BACKEND_URI.trim() === ''
        ? ensureBackslash(window.location.origin)
        : process.env.REACT_APP_BACKEND_URI;

export const BackendServiceUrlPath = ensureBackslash(window.location.pathname) + "socket.io"