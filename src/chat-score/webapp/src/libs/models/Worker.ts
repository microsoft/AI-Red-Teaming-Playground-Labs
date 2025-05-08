// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export interface SocketWorkerRequest {
  type: string;
  isSocket: boolean;
  payload: any;
}

export interface SocketWorkerResponse {
  type: string;
  isSocket: boolean;
  payload: string | undefined;
}

export interface WorkerInitRequestPayload {
  baseServiceUrl: string;
  baseServiceUrlPath: string;
}

export enum WorkerRequestType {
  Shutdown = "shutdown", //Shutdown the worker and close the connection
  Init = "init",
}

export enum WorkerResponseType {
  ShutdownComplete = "shutdown_complete", //Worker has closed the connection
}