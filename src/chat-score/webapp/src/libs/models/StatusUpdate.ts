// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ConversationStatus {
  id: number;
  challenge_id: number;
  in_review: boolean;
}

export interface StatusUpdate {
  session_count: number;
  conversation_queue: ConversationStatus[];
}