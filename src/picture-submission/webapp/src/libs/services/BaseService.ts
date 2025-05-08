// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function ensureBackslash(url: string): string {
  if (!url.endsWith('/'))
    return url + '/';
  return url;
}

function remove_first_backslash(url: string): string {
  if (url.startsWith('/'))
    return url.substring(1);
  return url;
}

const noResponseBodyStatusCodes = [202, 204];

interface ServiceRequest {
  commandPath: string;
  method?: string;
  body?: unknown;
  query?: URLSearchParams;
  blobResponse?: boolean;
}

interface AuthResponse {
  auth_type: string;
  redirect_uri: string;
  error: string;
}

export const BackendServiceUrl =
  process.env.REACT_APP_BACKEND_URI == null || process.env.REACT_APP_BACKEND_URI.trim() === ''
    ? ensureBackslash(document.URL)
  : process.env.REACT_APP_BACKEND_URI;

export const NetworkErrorMessage = '\n\nPlease check that your backend is running and that it is accessible by the app';
        
export class BaseService{ 
  constructor(protected readonly serviceUrl: string = BackendServiceUrl) { }
  
  protected readonly getResponseAsync = async<T>(request: ServiceRequest): Promise<T> => {
    const { commandPath, method, body, query } = request;
    const isFormData = body instanceof FormData;

    const headers = new Headers();

    if (!isFormData) {
      headers.append('Content-Type', 'application/json');
    }

    try {
      const requestUrl = new URL(this.serviceUrl + remove_first_backslash(commandPath));
      if (query) {
        requestUrl.search = `?${query.toString()}`;
      }

      const response = await fetch(requestUrl, {
        credentials: 'include', //Add support for cookies
        method: method ?? 'GET',
        body: isFormData ? body : JSON.stringify(body),
        headers
      });

      if (!response.ok) {
        if (response.status === 401) {
          const responseText = await response.text();

          // Redirect to the login page if the user is not authenticated
          const responseObject = JSON.parse(responseText) as AuthResponse;
          if (responseObject.auth_type && responseObject.auth_type === 'ctfd') {
            if (responseObject.redirect_uri) {
              const redirect_uri = responseObject.redirect_uri + `?next=${encodeURI(window.location.pathname)}`;
              window.location.href = redirect_uri;
              return {} as T;
            }
          }
        }else{
          throw new Error(response.statusText);
        }
      }

      if (request.blobResponse) {
        return await response.blob() as unknown as T;
      }

      return (noResponseBodyStatusCodes.includes(response.status) ? {} : await response.json()) as T;
    } catch (e: any) {
      let isNetworkError = false;
      if (e instanceof TypeError) {
          // fetch() will reject with a TypeError when a network error is encountered.
          isNetworkError = true;
      }
      throw Object.assign(new Error(`${e as string} ${isNetworkError ? NetworkErrorMessage : ''}`));
    }
  }
}