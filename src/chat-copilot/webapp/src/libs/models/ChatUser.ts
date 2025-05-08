// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface IChatUser {
    id: string;
    online: boolean;
    fullName: string;
    emailAddress: string;
    photo?: string; // TODO: [Issue #45] change this to required when we enable token / Graph support
    isTyping: boolean;
}
