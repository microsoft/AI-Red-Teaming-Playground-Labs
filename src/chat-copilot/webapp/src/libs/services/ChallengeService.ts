// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ChallengeSettings } from "../models/ChallengeSettings";
import { BaseService } from "./BaseService";

export class ChallengeService extends BaseService{
    public getSettingsAsync = async (accessToken: string): Promise<ChallengeSettings> => {
        return await this.getResponseAsync<ChallengeSettings>(
            {
                commandPath: "challenge/settings",
                method: 'GET'
            },
            accessToken
        );
    }

    public postXssAlertAsync = async (accessToken: string, chatId: string): Promise<void> => {
        await this.getResponseAsync(
            {
                commandPath: `chats/${chatId}/scoring/xss`,
                method: 'POST',
            },
            accessToken
        );
    }
}