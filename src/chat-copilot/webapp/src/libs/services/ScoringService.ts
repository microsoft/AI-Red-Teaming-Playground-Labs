// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ManualScoringResponse } from "../models/Scoring";
import { BaseService } from "./BaseService";

export class ScoringService extends BaseService {
  public createManualScoring = async (chatId: string, messageIndex: number, accessToken: string): Promise<ManualScoringResponse> => {
    const body = {
      chatId,
      messageIndex,
    };

    const result = await this.getResponseAsync<ManualScoringResponse>(
      {
        commandPath: `chats/${chatId}/scoring/manual`,
        method: 'POST',
        body,
      },
      accessToken,
    );

    return result;
  }
}