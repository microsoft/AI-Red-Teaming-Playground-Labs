// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ChatMemorySource {
    id: string;
    chatId: string;
    sourceType: string;
    name: string;
    hyperlink?: string;
    sharedBy: string;
    createdOn: number;
    size: number;
}
