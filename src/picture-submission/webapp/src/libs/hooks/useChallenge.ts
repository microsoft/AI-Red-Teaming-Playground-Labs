// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { useCallback, useMemo } from "react";
import { ChallengeService } from "../services/ChallengeService";

export const useChallenge = () => {
  const challengeService = useMemo(() => new ChallengeService(), []);

  const getChallengeSettings = useCallback(async () => {
      return await challengeService.getSettingsAsync();
    },
    [challengeService]
  );

  const getSubmissionStatus = useCallback(async () => {
      return await challengeService.getSubmissionStatusAsync();
    },
    [challengeService]
  );

  const submitPicture = useCallback(async (picture: Blob) => {
      return await challengeService.submitPictureAsync(picture);
    }, 
    [challengeService]
  );

  const getPicture = useCallback(async (id: string) => {
      return await challengeService.getPictureAsync(id);
    }, 
    [challengeService]
  );

  return {getChallengeSettings, getSubmissionStatus, submitPicture, getPicture};
}