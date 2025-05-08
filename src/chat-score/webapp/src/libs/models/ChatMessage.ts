// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export const REVIEW_TIME = 60; //Default time to review a conversation

export enum AuthorRoles {
  User = 0,
  Bot
}

export interface ChatMessage {
  message: string;
  role: AuthorRoles;
}

export interface ConversationReview{
  id: number;
  guid: string;
  title: string;
  goal: string;
  document: string | undefined;
  conversation: ChatMessage[] | undefined;
  picture: string | undefined;
}

export const enum SocketRequestType {
  Ping = "ping",
  ScoreConversation = "score_conversation",
  ActivitySignal = "activity_signal"
}

export const enum SocketResponseType {
  Connect = "connect",
  Disconnect = "disconnect",
  ServerError = "client_server_error",
  StatusUpdate = "client_status_update",
  ReviewUpdate = "client_review_update",
  ReviewDone = "client_review_done",
  TimeUpdate = "client_time_update"
}