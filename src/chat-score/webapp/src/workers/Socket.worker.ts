// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/* eslint-disable no-restricted-globals */
import { Socket, io } from 'socket.io-client';
import { SocketRequestType, SocketResponseType } from '../libs/models/ChatMessage';
import { SocketWorkerRequest, SocketWorkerResponse, WorkerInitRequestPayload, WorkerRequestType, WorkerResponseType } from '../libs/models/Worker';

const worker = () => {
  console.log('Worker started');
  let interval: ReturnType<typeof setInterval> | undefined = undefined;
  let socket: Socket | undefined = undefined;

  const sendWorkerMessage = (type: string, isSocket: boolean, payload: any = undefined) => {
    const data: SocketWorkerResponse = {
      type: type,
      isSocket: isSocket,
      payload: payload
    };
    self.postMessage(data);
  }

  const onConnect = () => {
    console.log('Worker received connect');
    sendWorkerMessage(SocketResponseType.Connect, true);

    //Send a ping every 5 seconds to keep the connection alive
    interval = setInterval(() => {
      socket!.emit(SocketRequestType.Ping);
    }, 5000);
  }

  const onDisconnect = () => {
    if (interval) {
      clearInterval(interval);
      interval = undefined;
    }

    console.log('Worker received disconnect');
    sendWorkerMessage(SocketResponseType.Disconnect, true);
  
    console.log("Retrying connection in 1 second");
    setTimeout(() => {
      socket!.connect();
    }, 1000);
  }

  const onError = (json: string) => {
    console.error('Worker received error:', json);
    sendWorkerMessage(SocketResponseType.ServerError, true, json);
  }

  const init = (initRequest: WorkerInitRequestPayload) => {
    console.log('Worker received init!');

    //Connect to the socket
    socket = io(initRequest.baseServiceUrl, {
      "path": initRequest.baseServiceUrlPath
    });
  
    socket!.on(SocketResponseType.Connect, onConnect);
    socket!.on(SocketResponseType.Disconnect, onDisconnect);
    socket!.on(SocketResponseType.ServerError, onError);

    //Forward the messages to the main thread
    const forwardedTypes = [
      SocketResponseType.StatusUpdate,
      SocketResponseType.ReviewUpdate,
      SocketResponseType.ReviewDone,
      SocketResponseType.TimeUpdate
    ];
  
    forwardedTypes.forEach(t => {
      socket!.on(t, (json: string) => {
        sendWorkerMessage(t, true, json);
      });
    });
  }

  const shutdown = () => {
    console.log('Worker received shutdown');
    socket!.off();
    socket!.disconnect();
    if (interval) {
      clearInterval(interval);
      interval = undefined;
    }

    sendWorkerMessage(WorkerResponseType.ShutdownComplete, false);
  }

  self.addEventListener('message', (event) => {
    const data: SocketWorkerRequest = event.data;
    console.log('Worker received message:', data);
    if (!data.isSocket) {
      switch (data.type) {
        case WorkerRequestType.Init:
          init(data.payload as WorkerInitRequestPayload);
          break;
        case WorkerRequestType.Shutdown:
          shutdown();
          break;
      }
    } else {
      if (data.payload) {
        socket!.emit(data.type, data.payload);
      } else {
        socket!.emit(data.type);
      }
    }
  });
}
worker();
export default worker;