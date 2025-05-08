// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { IChatMessage } from '../../../libs/models/ChatMessage';
import { IChatUser } from '../../../libs/models/ChatUser';

export interface ChatState {
    id: string;
    title: string;
    systemDescription: string;
    memoryBalance: number;
    users: IChatUser[];
    messages: IChatMessage[];
    enabledHostedPlugins: string[];
    botProfilePicture: string;
    lastUpdatedTimestamp?: number;
    input: string;
    botResponseStatus: string | undefined;
    userDataLoaded: boolean;
    ragDocument: string;
    ragUserInput?: string;
    importingDocuments?: string[];
    disabled: boolean; // For labeling a chat has been deleted
    hidden: boolean; // For hiding a chat from the list
    locked: boolean; // For locking a chat
    maxTurnReached: boolean; //The conversation has reached the maximum number of turns
}
