// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ChallengeSettings{
    id: number;
    name: string;
    description: string;
    metapromptLeak: boolean;
    fileUpload: boolean;
    plugins: boolean;
    pluginsControl: boolean;
    humanScorer: boolean;
    autoScorer: boolean;
    planEdit: boolean;
    xssVulnerable: boolean;
    ragInput: RagInputSettings;
    backNavigation: boolean;
}

export interface RagInputSettings{
    enabled: boolean;
    document: string;
    template: string;
    isReadOnly: boolean;
    titleShort: string;
    titleLong: string;
    instruction1: string;
    instruction2: string;
    firstMessage: string;
    maxTurns: number;
}