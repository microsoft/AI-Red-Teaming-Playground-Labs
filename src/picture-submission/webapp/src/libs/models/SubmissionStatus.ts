// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export enum ReviewStatus {
    READY = "ready",
    REVIEWING = "reviewing",
    REVIEWED = "reviewed",
  }
  
  export interface ScoringResultResponse {
    passed: boolean;
    message: string;
    flag: string | null;
  }
  
  export interface SubmissionStatus {
    picture_id: string;
    status: ReviewStatus;
    scoring_result: ScoringResultResponse | null;
  }