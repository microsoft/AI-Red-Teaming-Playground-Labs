// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ConversationReview } from "../../../libs/models/ChatMessage";
import { ConversationStatus } from "../../../libs/models/StatusUpdate";

export interface AppState {
  connectionCount: number;
  connected: boolean;
  conversationReview: ConversationReview | null;
  conversationQueue: ConversationStatus[];
  timeRemaining: number;
}

export const initialState: AppState = {
  connectionCount: 0,
  connected: false,
  conversationReview: null,
  conversationQueue: [],
  timeRemaining: 0
};