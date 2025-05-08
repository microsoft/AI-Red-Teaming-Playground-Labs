// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { makeStyles, Spinner, tokens } from "@fluentui/react-components";
import React, { FC, useState } from "react";
import useLocalStorageState, { LocalStorageState } from "use-local-storage-state";
import { AuthorRoles } from "../../libs/models/ChatMessage";
import { ISettings, SETTINGS_KEY_NAME } from "../../libs/models/Settings";
import { hasInteracted } from "../../libs/utils/UserInteraction";
import { useAppSelector } from "../../redux/app/hooks";
import { RootState } from "../../redux/app/store";
import { IMessageProps, Message } from "./Message";

export const useClasses = makeStyles({
  root: {
    backgroundColor: tokens.colorNeutralBackground3,
    height: '100%',
    overflow: 'auto',
  },
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  containerSpinner: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
  },
  messages: {
    //width: '100%',
    height: "100%",
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingHorizontalL,
    gap: tokens.spacingVerticalS,
    maxWidth: '900px',
    width: '80%'
  }
});
const notificationSound = new Audio("./chime.mp3");

export const ConversationView: FC = () => {
  const classes = useClasses();
  const appState = useAppSelector((state: RootState) => state.app);
  const conversationReview = useAppSelector((state: RootState) => state.app.conversationReview);

  const [conversation, setConversation] = useState<IMessageProps[]>([]);
  const [settings] = useLocalStorageState(SETTINGS_KEY_NAME) as LocalStorageState<ISettings>;

  React.useEffect(() => {
    if (conversationReview) {
      const tempConversation: IMessageProps[] = [];
      if (conversationReview.conversation) {
        conversationReview.conversation.forEach((message) => {
          const tempMessage = {
            role: message.role,
            message: message.message,
            document: "",
            picture: ""
          }
          if (tempConversation.length < 1) {
            tempMessage.document = conversationReview.document ?? "";
          }
          tempConversation.push(tempMessage);
        });
      } else {
        const tempMessage = {
          role: AuthorRoles.User,
          message: "",
          document: "",
          picture: conversationReview.picture!
        };
        tempConversation.push(tempMessage);
      }

      setConversation(tempConversation);
      hasInteracted() && settings.enableSound && notificationSound.play();
    } else {
      setConversation([]);
    }
  }, [conversationReview, settings.enableSound]);

  return (
    <div className={classes.root}>
      {conversationReview && (
        <div className={classes.container}>
          <div className={classes.messages}>
            {conversation.map((message, index) => (
              <Message key={index} role={message.role} message={message.message} document={message.document ?? ""} picture={message.picture}></Message>
            ))}
          </div>
        </div>
      )}
      {!conversationReview && (
        <div className={classes.containerSpinner}>
          <Spinner size="large" label={appState.connected ? "Waiting for assignment..." : "Connecting to backend..."} />
        </div>
      )}

    </div>
  )
};