// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ChallengeSettings } from "../../../libs/models/ChallengeSettings";
import { ReviewStatus, SubmissionStatus } from "../../../libs/models/SubmissionStatus";

export interface AppState {
  challengeSettings: ChallengeSettings;
  status: SubmissionStatus;
  picture: string | null;
}

export const initialState: AppState = {
  challengeSettings: {
    id: 0,
    name: '',
    description: '',
  },
  status: {
    picture_id: '',
    status: ReviewStatus.READY,
    scoring_result: null,
  },
  picture: null,
}