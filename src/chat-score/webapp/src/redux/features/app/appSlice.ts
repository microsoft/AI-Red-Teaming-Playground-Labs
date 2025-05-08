// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { ConversationReview } from "../../../libs/models/ChatMessage";
import { ConversationStatus } from "../../../libs/models/StatusUpdate";
import { AppState, initialState } from "./AppState";

export const appSlice = createSlice({
  name: "app",
  initialState,
  reducers: {
    setConnectionCount: (state: AppState, action: PayloadAction<number>) => {
      state.connectionCount = action.payload;
    },
    setConnectionStatus: (state: AppState, action: PayloadAction<boolean>) => {
      state.connected = action.payload;
    },
    setConversationStatus: (state: AppState, action: PayloadAction<ConversationStatus[]>) => {
      state.conversationQueue = action.payload;
    },
    setConversationReview: (state: AppState, action: PayloadAction<ConversationReview | null>) => {
      state.conversationReview = action.payload;
    },
    setTime: (state: AppState, action: PayloadAction<number>) => {
      state.timeRemaining = action.payload;
    }
  }
});

export const {
  setConnectionCount,
  setConnectionStatus,
  setConversationStatus,
  setConversationReview,
  setTime} = appSlice.actions;

export default appSlice.reducer;