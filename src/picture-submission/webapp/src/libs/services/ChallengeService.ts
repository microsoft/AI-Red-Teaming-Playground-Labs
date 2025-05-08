// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { ChallengeSettings } from "../models/ChallengeSettings";
import { SubmissionStatus } from "../models/SubmissionStatus";
import { BaseService } from "./BaseService";

export class ChallengeService extends BaseService{
  public getSettingsAsync = async (): Promise<ChallengeSettings> => {
    return await this.getResponseAsync<ChallengeSettings>({
      commandPath: "challenge/settings",
      method: 'GET'
    });
  }

  public getSubmissionStatusAsync = async (): Promise<SubmissionStatus> => {
    return await this.getResponseAsync<SubmissionStatus>({
      commandPath: "/status",
      method: 'GET'
    });
  }

  public getPictureAsync = async (id: string): Promise<Blob> => {
    return await this.getResponseAsync<Blob>({
      commandPath: `/picture?id=${id}`,
      method: 'GET',
      blobResponse: true
    });
  }

  public submitPictureAsync = async (picture: Blob): Promise<void> => {
    //FormData from Blob
    const form = new FormData();
    form.append('file', picture);
    return await this.getResponseAsync({
      commandPath: "/upload",
      method: 'POST',
      body: form
    });
  }
}