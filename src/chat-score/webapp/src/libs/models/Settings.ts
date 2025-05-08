// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface ISettings {
  enableSound: boolean;
  darkMode: boolean;
  standardReplies: string[];
}

export const defaultSettings: ISettings = {
  enableSound: true,
  darkMode: false,
  standardReplies: [],
};

export const SETTINGS_KEY_NAME = "chat-score-settings";