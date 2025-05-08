// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { ChallengeSettings } from "../../../libs/models/ChallengeSettings";
import { SubmissionStatus } from "../../../libs/models/SubmissionStatus";
import { AppState, initialState } from "./AppState";

export const appSlice = createSlice({
  name: "app",
  initialState,
  reducers: {
    setChallengeSettings: (state: AppState, action: PayloadAction<ChallengeSettings>) => {
      state.challengeSettings = action.payload;
    },
    setStatus: (state: AppState, action: PayloadAction<SubmissionStatus>) => {
      state.status = action.payload;
    },
    setPicture:(state: AppState, action: PayloadAction<string>) => {
      state.picture = action.payload;
    },
  }
});

export const { setChallengeSettings, setStatus, setPicture } = appSlice.actions;

export default appSlice.reducer;
