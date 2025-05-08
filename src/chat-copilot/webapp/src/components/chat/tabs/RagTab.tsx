// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import React from "react";

import {
    Button,
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
    DialogSurface,
    DialogTitle,
    makeStyles,
    MessageBar,
    MessageBarBody,
    MessageBarTitle,
    Spinner,
    Textarea
} from '@fluentui/react-components';
import { useChat } from "../../../libs/hooks/useChat";
import { AlertType } from "../../../libs/models/AlertType";
import { ChatMessageType } from "../../../libs/models/ChatMessage";
import { useAppDispatch, useAppSelector } from "../../../redux/app/hooks";
import { RootState } from "../../../redux/app/store";
import { addAlert } from "../../../redux/features/app/appSlice";
import { editConversationRag } from "../../../redux/features/conversations/conversationsSlice";
import { TabView } from "./TabView";


const useClasses = makeStyles({
    goChat: {
        paddingTop: '1em',
        float: 'right',
        marginLeft: 'auto',
        marginRight: 0
    },
});


interface RagTabProps {
    onTabChange: (tabName: string) => void;
}

export const RagTab: React.FC<RagTabProps> = ({ onTabChange }) => {
    const chat = useChat();

    const ragInput = useAppSelector((state: RootState) => state.app.challengeSettings.ragInput)
    const { conversations, selectedId, } = useAppSelector((state: RootState) => state.conversations);
    const dispatch = useAppDispatch();

    const [documentValue, setDocumentValue] = React.useState<string>("");
    const [userContentValue, setUserContentValue] = React.useState<string>("");
    const [currentSelectedId, setSelectedId] = React.useState<string>("");
    const [loadingMessage, setLoadingMessage] = React.useState<string>("");
    const [showDialog, setShowDialog] = React.useState(false);
    const [showErrorDialog, setShowErrorDialog] = React.useState(false);
    const [isLoading, setIsLoading] = React.useState(false);
    const [canEdit, setCanEdit] = React.useState(true);




    React.useEffect(() => {
        if (conversations[selectedId].ragDocument) {
            setDocumentValue(conversations[selectedId].ragDocument);
        } else if (currentSelectedId !== selectedId) {
            //We need to set the default value of the document at the very least
            dispatch(
                editConversationRag({
                    id: selectedId,
                    document: ragInput.document,
                    userInput: "",
                })
            );
            setDocumentValue(ragInput.document);
        }

        const ragUserInput = conversations[selectedId].ragUserInput;
        if (ragUserInput) {
            setUserContentValue(ragUserInput);
        } else {
            setUserContentValue("");
        }

        if (currentSelectedId !== selectedId) {
            setSelectedId(selectedId);
        }

        if (conversations[selectedId].messages.length > 1) {
            setCanEdit(false);
        } else {
            setCanEdit(true);
        }
    }, [selectedId, conversations, ragInput.document, currentSelectedId, dispatch]);


    const goToChat = () => {
        if (canEdit) {
            if (ragInput.isReadOnly && conversations[selectedId].ragUserInput === "") {
                setShowErrorDialog(true);
                return;
            } else if (!ragInput.isReadOnly && conversations[selectedId].ragDocument === ragInput.document) {
                setShowErrorDialog(true);
                return;
            }
            setShowDialog(true);
        } else {
            onTabChange("chat");
        }
    }

    const submitDocument = () => {
        setLoadingMessage("Sending content...");
        setIsLoading(true);
        chat.editRag(selectedId, conversations[selectedId].ragDocument, conversations[selectedId].ragUserInput).then(() => {
            setLoadingMessage("Generating first message...");

            //Send the first message
            chat.getResponse({ messageType: ChatMessageType.Message, value: ragInput.firstMessage, chatId: selectedId }).then(() => {
                setIsLoading(false);
                setShowDialog(false);
                onTabChange("chat");
            }).catch((error: Error) => {
                setShowDialog(false);
                dispatch(addAlert({ message: error.message, type: AlertType.Error }));
            });
        }).catch((error: Error) => {
            setShowDialog(false);
            dispatch(addAlert({ message: error.message, type: AlertType.Error }));
        });
    }

    const classes = useClasses();

    return (

        <TabView
            title={ragInput.titleLong}
            learnMoreDescription=""
            learnMoreLink="">
            {!canEdit && (
                <>
                    <MessageBar key="warning" intent="warning">
                        <MessageBarBody>
                            <MessageBarTitle>Conversation started</MessageBarTitle>
                            The conversation has started. You can no longer edit the document. To edit the document, please start a new conversation.
                        </MessageBarBody>
                    </MessageBar>
                </>
            )}
            <p>
                {ragInput.instruction1}
            </p>
            <Textarea
                resize="vertical"
                value={documentValue}
                placeholder={ragInput.document === "" ? "Please enter the content description here" : ""}
                rows={40}
                disabled={ragInput.isReadOnly as boolean || !canEdit}
                onChange={(_event, data) => {
                    setDocumentValue(data.value);
                    dispatch(editConversationRag({
                        id: selectedId,
                        document: data.value,
                        userInput: userContentValue,
                    }))
                }}
            />

            {ragInput.isReadOnly && (
                <>
                    <p>
                        {ragInput.instruction2}
                    </p>
                    <Textarea
                        resize="vertical"
                        placeholder="Enter your user content here..."
                        rows={4}
                        value={userContentValue}
                        disabled={!canEdit}
                        onChange={(_event, data) => {
                            const newDocumentValue = updateDocumentUserInput(ragInput.template, ragInput.document, data.value);
                            setUserContentValue(data.value);
                            setDocumentValue(newDocumentValue);
                            dispatch(editConversationRag({
                                id: selectedId,
                                document: newDocumentValue,
                                userInput: data.value,
                            }))
                        }}
                    />
                </>
            )}
            <div className={classes.goChat}>
                <Button appearance="primary" onClick={goToChat}>
                    {canEdit ? "Test it in Chat" : "See Chat"}
                </Button>
            </div>
            <Dialog open={showDialog}>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>Confirm</DialogTitle>
                        <DialogContent>
                            {(isLoading) && (
                                <Spinner label={loadingMessage} size='tiny' />
                            )}

                            {!isLoading && (
                                <>
                                    Do you want to submit this document you will not be able to edit it after submission and will have to create a new chat to try again?
                                </>
                            )}

                        </DialogContent>
                        <DialogActions>
                            <Button appearance="secondary" onClick={() => { setShowDialog(false) }} disabled={isLoading}>Close</Button>
                            <Button appearance="primary" onClick={submitDocument} disabled={isLoading}>Confirm</Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>

            <Dialog open={showErrorDialog}>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>Content did not change</DialogTitle>
                        <DialogContent>
                            The content did not change. Please make some changes before submitting.
                        </DialogContent>
                        <DialogActions>
                            <Button appearance="primary" onClick={() => { setShowErrorDialog(false) }}>Ok</Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>
        </TabView>
    );
};

export function updateDocumentUserInput(template: string, document: string, userInput: string) {
    if (userInput === "") {
        return document;
    }

    const updatedDocument = document.replace(template, userInput);
    return updatedDocument;
}