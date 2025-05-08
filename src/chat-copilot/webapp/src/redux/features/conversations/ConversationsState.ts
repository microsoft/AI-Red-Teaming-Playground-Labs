// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IChatMessage } from '../../../libs/models/ChatMessage';
import { ChatState } from './ChatState';

export type Conversations = Record<string, ChatState>;

export interface ConversationsState {
    conversations: Conversations;
    selectedId: string;
    total: number;
    loadedCount: number;
}

export const initialState: ConversationsState = {
    conversations: {},
    selectedId: '',
    total: 0,
    loadedCount: 0
};

export interface UpdateConversationPayload {
    id: string;
    messages: IChatMessage[];
}

export interface ConversationTitleChange {
    id: string;
    newTitle: string;
}

export interface ConversationInputChange {
    id: string;
    newInput: string;
}

export interface ConversationSystemDescriptionChange {
    id: string;
    newSystemDescription: string;
}

export interface ConversationLockedChange {
    id: string;
    newLocked: boolean;
}

export interface ConversationMaxTurnReachedChange{
    id: string;
    newMaxTurnReached: boolean;
}

export interface ConversationRagChange{
    id: string;
    document: string;
    userInput: string;
}

export interface UpdatePluginStatePayload {
    id: string;
    pluginName: string;
    newState: boolean;
}