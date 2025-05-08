// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FluentProvider, makeStyles, teamsDarkTheme, teamsLightTheme, Toast, ToastBody, Toaster, ToastTitle, tokens, useToastController } from '@fluentui/react-components';
import { useEffect } from 'react';
import useLocalStorageState from 'use-local-storage-state';
import './App.css';
import { ConversationQueue } from './components/ConversationQueue';
import { Header } from './components/Header';
import { MainView } from './components/conversation/MainView';
import { ConversationReview, REVIEW_TIME, SocketResponseType } from './libs/models/ChatMessage';
import { ErrorMessage } from './libs/models/ErrorMessage';
import { defaultSettings, SETTINGS_KEY_NAME } from './libs/models/Settings';
import { StatusUpdate } from './libs/models/StatusUpdate';
import { SocketWorkerResponse, WorkerInitRequestPayload, WorkerRequestType, WorkerResponseType } from './libs/models/Worker';
import { BackendServiceUrl, BackendServiceUrlPath } from './libs/services/BaseService';
import { dispatchWorkerMessage, worker } from './libs/services/WorkerService';
import { setupInteractionDetector } from './libs/utils/UserInteraction';
import { useAppDispatch } from './redux/app/hooks';
import { setConnectionCount, setConnectionStatus, setConversationReview, setConversationStatus, setTime } from './redux/features/app/appSlice';

export const useClasses = makeStyles({
  root: {
    backgroundColor: tokens.colorNeutralBackground4,
  }
})

function App() {
  const classes = useClasses();
  const dispatch = useAppDispatch();

  const { dispatchToast } = useToastController("fu-toast");

  const [settings] = useLocalStorageState(SETTINGS_KEY_NAME, {
    defaultValue: defaultSettings
  });

  //TODO move this to a hook
  useEffect(() => {

    const notify = (connectionState: boolean) => {
      if (connectionState) {
        dispatchToast(
          <Toast>
            <ToastTitle>Connection established</ToastTitle>
            <ToastBody>The connection to the server was established.</ToastBody>
          </Toast>
          , { intent: "success" }
        );
      } else {
        dispatchToast(
          <Toast>
            <ToastTitle>Connection disconnected</ToastTitle>
            <ToastBody>The connection to the server disconnected.</ToastBody>
          </Toast>
          , { intent: "error" }
        );
      }
    }

    function onConnect() {
      dispatch(setConnectionStatus(true))
      notify(true);
    }

    function onDisconnect() {
      dispatch(setConnectionStatus(false))
      dispatch(setConversationReview(null))
      notify(false)
    }

    function onStatusUpdate(json: string) {
      const data = JSON.parse(json) as StatusUpdate
      console.log("Status Update", data);

      dispatch(setConnectionCount(data.session_count));
      dispatch(setConversationStatus(data.conversation_queue));
    }

    function onReview(json: string) {
      const data = JSON.parse(json) as ConversationReview
      console.log("Review Update", data);

      dispatch(setConversationReview(data))
      dispatch(setTime(REVIEW_TIME));
    }

    function onReviewDone() {
      console.log("Review Done");
      dispatch(setConversationReview(null));
      // We set the time at zero
      dispatch(setTime(0));
    }

    function onTimeUpdate(data: string) {
      const time = Number(data)
      console.log("Time Update", time)
      dispatch(setTime(time));
    }

    function onError(json: string) {
      const data = JSON.parse(json) as ErrorMessage
      dispatchToast(
        <Toast>
          <ToastTitle>Server Error</ToastTitle>
          <ToastBody>{data.error_msg}</ToastBody>
        </Toast>
        , { intent: "error" }
      );
    }

    setupInteractionDetector();

    worker.addEventListener('message', (event: any) => {
      const data = event.data as SocketWorkerResponse;
      console.log('Main thread received message:', data);
      if (data.isSocket) {
        switch (data.type) {
          case SocketResponseType.Connect:
            onConnect();
            break;
          case SocketResponseType.Disconnect:
            onDisconnect();
            break;
          case SocketResponseType.StatusUpdate:
            onStatusUpdate(data.payload!);
            break;
          case SocketResponseType.ReviewUpdate:
            onReview(data.payload!);
            break;
          case SocketResponseType.ReviewDone:
            onReviewDone();
            break;
          case SocketResponseType.TimeUpdate:
            onTimeUpdate(data.payload!);
            break;
          case SocketResponseType.ServerError:
            onError(data.payload!);
            break;
          default:
            console.error("Unknown message type", data);
        }
      } else {
        switch (data.type) {
          case WorkerResponseType.ShutdownComplete:
            console.log("Worker has shutdown");
            worker.terminate();
            break;
          default:
            console.error("Unknown message type", data);
        }
      }
    });

    //Mark the UI as ready to receive messages
    dispatchWorkerMessage(WorkerRequestType.Init,{
      baseServiceUrl: BackendServiceUrl,
      baseServiceUrlPath: BackendServiceUrlPath
    } as WorkerInitRequestPayload);

    return () => {
      console.log("Cleanup component")
      dispatchWorkerMessage(WorkerRequestType.Shutdown);
    }
  }, [dispatch, dispatchToast]);

  return (
    <FluentProvider theme={settings.darkMode ? teamsDarkTheme : teamsLightTheme}>
      <div className={"App " + classes.root}>
        <Header></Header>
        <div className="main-app">
          <MainView></MainView>
          <ConversationQueue></ConversationQueue>
        </div>
      </div>
      <Toaster toasterId="fu-toast"></Toaster>
    </FluentProvider>
  );
}

export default App;
