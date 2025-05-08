// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// @ts-ignore
import SocketWorker from '../../workers/Socket.worker.ts';
import { SocketWorkerRequest } from '../models/Worker.js';

// @ts-ignore
export const worker: Worker = new SocketWorker();


export function dispatchWorkerMessage(messageName: string, payload: any = undefined) {
  const message: SocketWorkerRequest = {
    type: messageName,
    isSocket: false,
    payload: payload
  };
  worker.postMessage(message);
}

export function dispatchSocketMessage(messageName: string, payload: any = undefined) {
  const message: SocketWorkerRequest = {
    type: messageName,
    isSocket: true,
    payload: payload
  };
  worker.postMessage(message);
}