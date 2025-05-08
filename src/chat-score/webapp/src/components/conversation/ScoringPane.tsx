// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Accordion, AccordionHeader, AccordionItem, AccordionPanel, Body1, Body1Strong, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle, makeStyles, Menu, MenuButtonProps, MenuItem, MenuList, MenuPopover, MenuTrigger, SplitButton, Textarea, tokens } from "@fluentui/react-components";
import { Checkmark20Regular, Dismiss20Regular } from "@fluentui/react-icons";
import { FC, useCallback, useEffect, useState } from "react";
import useLocalStorageState, { LocalStorageState } from "use-local-storage-state";
import { REVIEW_TIME, SocketRequestType } from "../../libs/models/ChatMessage";
import { ISettings, SETTINGS_KEY_NAME } from "../../libs/models/Settings";
import { dispatchSocketMessage } from "../../libs/services/WorkerService";
import { ACTIVITY_THRESHOLD, isUserActive } from "../../libs/utils/UserInteraction";
import { useAppDispatch, useAppSelector } from "../../redux/app/hooks";
import { setConversationReview } from "../../redux/features/app/appSlice";
import { StandardRepliesTable } from "../StandardRepliesTable";

export const useClasses = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between',
    alignItems: 'center',
    width: '200px',
    padding: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground4,
    boxShadow: tokens.shadow4,
  },
  buttonGroup: {
    textAlign: 'center',
  },
  buttonSpacing: {
    marginBottom: tokens.spacingVerticalM,
  },
  buttonWidth: {
    width: '100%',
  },
  textarea: {
    width: '100%',
    marginTop: tokens.spacingVerticalS,
  },
  backgroundFail: {
    backgroundColor: tokens.colorStatusDangerBackground1,
  },
  backgroundPass: {
    backgroundColor: tokens.colorStatusSuccessBackground1,
  },
  accordion: {
    paddingBottom: tokens.spacingVerticalS,
  },
  standardReplies: {
    color: tokens.colorNeutralForeground2,
  },
  feedbackSection: {
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
  },
  buttonSendPadding: {
    marginRight: tokens.spacingHorizontalM,
  }
});

export const ScoringPane: FC = () => {
  const classes = useClasses();
  const dispatch = useAppDispatch();

  const conversationReview = useAppSelector((state) => state.app.conversationReview);
  const timeRemaining = useAppSelector((state) => state.app.timeRemaining);
  const disabled = !conversationReview;

  const [settings] = useLocalStorageState(SETTINGS_KEY_NAME) as LocalStorageState<ISettings>;
  const [showMessage, setShowMessage] = useState(false);
  const [passing, setPassing] = useState(false);
  const [feedback, setFeedback] = useState('');
  const [timer, setTimer] = useState(REVIEW_TIME);
  const [repliesCollapsed, setRepliesCollapsed] = useState(false);

  const hasReplies = settings.standardReplies.length > 0;


  const onToggle = useCallback(() => {
    setRepliesCollapsed(!repliesCollapsed);
  }, [repliesCollapsed, setRepliesCollapsed]);

  const sendScore = (passed: boolean, customMessage: string) => {
    dispatchSocketMessage(SocketRequestType.ScoreConversation, {
      'conversation_id': conversationReview?.guid,
      'passed': passed,
      'custom_message': customMessage
    });
    dispatch(setConversationReview(null));
  }

  const pass = () => {
    setPassing(true);
    setShowMessage(true);
  }

  const passQuick = () => {
    sendScore(true, '');
  }

  const fail = () => {
    setPassing(false);
    setShowMessage(true);
  }

  const cancel = () => {
    setShowMessage(false);
  }
  const submit = () => {
    sendScore(passing, feedback);
    setShowMessage(false);
  }

  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | null = null;
    if (conversationReview) {
      interval = setInterval(() => {
        setTimer((prev) => {
          let newTime = prev - 1;
          if (newTime < 0) {
            newTime = 0;
          }

          //Should we send an activity update?
          if (prev > 0 && newTime % ACTIVITY_THRESHOLD === 0) {
            if (isUserActive()) {
              console.log("User is still active will send an activity update")
              dispatchSocketMessage(SocketRequestType.ActivitySignal);
            } else {
              console.log("User is not marked as active")
            }
          }
          return newTime;
        });
      }, 1000);
    }

    //We make it ready for the new conversation to review
    setShowMessage(false);
    setFeedback('');
    return () => {
      if (interval) {
        clearInterval(interval);
      }
    }
  }, [conversationReview]);


  useEffect(() => {
    if (conversationReview) {
      //We only care about the time when a conversation is displayed
      setTimer(timeRemaining);
    }
  }, [conversationReview, timeRemaining]);

  return (
    <div className={classes.root}>
      {conversationReview && (
        <Body1>{timer}s remaining</Body1>
      )}
      {!conversationReview && (
        <p></p>
      )}
      <div className={classes.buttonGroup}>
        <div>
          <Menu positioning="below-end">
            <MenuTrigger disableButtonEnhancement>
              {(triggerProps: MenuButtonProps) => (
                <SplitButton
                  menuButton={triggerProps}
                  icon={<Checkmark20Regular />}
                  disabled={disabled}
                  primaryActionButton={{'onClick': passQuick}}
                  className={classes.buttonSpacing}
                >
                  Pass
                </SplitButton>
              )}
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={pass}>Pass with feedback</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
        <Button className={classes.buttonWidth} icon={<Dismiss20Regular />} disabled={disabled} onClick={fail}>Fail</Button>
      </div>
      <Dialog open={showMessage}>
        <DialogSurface className={passing ? classes.backgroundPass : classes.backgroundFail}>
          <DialogBody>
            <DialogTitle>Leave feedback?</DialogTitle>
            <DialogContent>
              {hasReplies &&
                <Accordion collapsible defaultOpenItems={!repliesCollapsed ? ['replies']: []} className={classes.accordion} onToggle={onToggle}>
                  <AccordionItem value="replies">
                    <AccordionHeader>
                      <Body1Strong className={ classes.standardReplies}>Standard Replies</Body1Strong>
                    </AccordionHeader>
                    <AccordionPanel>
                      <StandardRepliesTable onSelectReply={(reply) => setFeedback(reply)} />
                    </AccordionPanel>
                  </AccordionItem>
                </Accordion>
              }
              <div className={hasReplies ? classes.feedbackSection : ""}>
                <Body1>Current Grade: {passing ? "Pass" : "Fail"}</Body1>
                <Textarea
                  className={classes.textarea}
                  value={feedback}
                  placeholder="Enter your feedback here..."
                  onChange={(_, data) => setFeedback(data.value)}>
                </Textarea>
              </div>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={cancel}>Cancel</Button>
              <Button appearance="primary" onClick={submit} className={hasReplies ? classes.buttonSendPadding : ""}>Send</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
};