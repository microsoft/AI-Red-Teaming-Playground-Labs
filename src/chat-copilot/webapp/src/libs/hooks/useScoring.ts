// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { useMsal } from "@azure/msal-react";
import React from "react";
import { AuthHelper } from "../auth/AuthHelper";
import { ScoringService } from "../services/ScoringService";


export const useScoring = () => {
  const { instance, inProgress } = useMsal();
  const scoringService = React.useMemo(() => new ScoringService(), []);

  const createManualScoring = React.useCallback(
    async (chatId: string, messageIndex: number) => {
      const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
      return await scoringService.createManualScoring(chatId, messageIndex, accessToken);
    },
    [scoringService, inProgress, instance],
  )

  return {
    createManualScoring
  }
}