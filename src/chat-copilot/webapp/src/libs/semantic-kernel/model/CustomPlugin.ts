// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ICustomPlugin {
    nameForHuman: string;
    nameForModel: string;
    authHeaderTag: string;
    authType: string;
    manifestDomain: string;
}
