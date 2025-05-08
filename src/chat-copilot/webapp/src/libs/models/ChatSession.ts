// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IChatMessage } from './ChatMessage';

export interface IChatSession {
    id: string;
    title: string;
    systemDescription: string;
    memoryBalance: number;
    enabledPlugins: string[];
    locked: boolean;
    ragDocument?: string;
    ragUserInput?: string;
    maxTurnReached: boolean;
}

export interface ICreateChatSessionResponse {
    chatSession: IChatSession;
    initialBotMessage: IChatMessage;
}
