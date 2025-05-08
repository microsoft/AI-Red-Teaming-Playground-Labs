// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IChatMessage } from './ChatMessage';

export interface ChatArchive {
    Schema: { Name: string; Version: number };
    Configurations: { EmbeddingAIService: string; EmbeddingDeploymentOrModelId: string };
    ChatTitle: string;
    ChatHistory: IChatMessage[];
    Embeddings: any[]; // TODO: [Issue #47] Add type. See Bot.cs
}
