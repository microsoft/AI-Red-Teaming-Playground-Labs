// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { useMsal } from "@azure/msal-react";
import React from "react";
import { AuthHelper } from "../auth/AuthHelper";
import { ChallengeService } from "../services/ChallengeService";

export const useChallenge = () => {
    const { instance, inProgress } = useMsal();
    const challengeService = React.useMemo(() => new ChallengeService(), []);

    const getChallengeSettings = React.useCallback(
        async () => {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            return await challengeService.getSettingsAsync(accessToken);
        },
        [challengeService, inProgress, instance],
    )

    const postXssAlert = React.useCallback(
        async (chatId: string) => {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await challengeService.postXssAlertAsync(accessToken, chatId);
        },
        [challengeService, inProgress, instance],
    )

    return {
        getChallengeSettings,
        postXssAlert
    }
}