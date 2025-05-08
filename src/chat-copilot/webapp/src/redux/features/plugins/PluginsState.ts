// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


export const enum AuthHeaderTags {
    MsGraph = 'graph',
    Jira = 'jira',
    GitHub = 'github',
}

export interface PluginAuthRequirements {
    username?: boolean;
    email?: boolean;
    password?: boolean;
    personalAccessToken?: boolean;
    OAuth?: boolean;
    Msal?: boolean;
    scopes?: string[];
    helpLink?: string;
}

// Additional information required to enable OpenAPI functions, i.e., server-url
// Key should be the property name and in kebab case (valid format for request header),
// make sure it matches exactly with the property name the API requires
export type AdditionalApiProperties = Record<
    string,
    {
        required: boolean;
        helpLink?: string;
        value?: string;
        description?: string;
    }
>;

export interface Plugin {
    name: string;
    nameForModel?: string;
    publisher: string;
    description: string;
    enabled: boolean;
    authRequirements: PluginAuthRequirements;
    headerTag: AuthHeaderTags | string;
    icon: string; // Can be imported as shown above or direct URL
    authData?: string; // token or encoded auth header value
    apiProperties?: AdditionalApiProperties;
    manifestDomain?: string; // Website domain hosting the OpenAI Plugin Manifest file for custom plugins
}

export interface PluginsState {
    plugins: Plugins;
}

export type Plugins = Record<string, Plugin>;

export const initialState: PluginsState = {
    plugins: {
    },
};

export interface EnablePluginPayload {
    plugin: string;
    username?: string;
    email?: string;
    password?: string;
    accessToken?: string;
    apiProperties?: AdditionalApiProperties;
}
