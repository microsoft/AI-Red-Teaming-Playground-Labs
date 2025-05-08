// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import React from 'react';
import { GetResponseOptions, useChat } from '../../libs/hooks/useChat';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { FeatureKeys, Features } from '../../redux/features/app/AppState';
import { editConversationMaxTurnReached } from '../../redux/features/conversations/conversationsSlice';
import { SharedStyles } from '../../styles';
import { ChatInput, ChatInputState } from './ChatInput';
import { ChatHistory } from './chat-history/ChatHistory';

const useClasses = makeStyles({
    root: {
        ...shorthands.overflow('hidden'),
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        height: '100%',
    },
    scroll: {
        ...shorthands.margin(tokens.spacingVerticalXS),
        ...SharedStyles.scroll,
    },
    history: {
        ...shorthands.padding(tokens.spacingVerticalM),
        marginLeft: '40px',
        paddingRight: '40px',
        display: 'flex',
        justifyContent: 'center',
    },
    input: {
        ...shorthands.padding(tokens.spacingVerticalM),
    },
});

const isSkippable = (message: string | undefined) => {
    return message === "Generating semantic chat memory" || message === "Calculating token usage" || message === "" || !message;
}

export const ChatRoom: React.FC = () => {
    const classes = useClasses();
    const chat = useChat();

    const dispatch = useAppDispatch();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { features } = useAppSelector((state: RootState) => state.app);
    const { ragInput } = useAppSelector((state: RootState) => state.app.challengeSettings);
    const messages = conversations[selectedId].messages;

    const scrollViewTargetRef = React.useRef<HTMLDivElement>(null);
    const [shouldAutoScroll, setShouldAutoScroll] = React.useState(true);
    const [isDraggingOver, setIsDraggingOver] = React.useState(false);

    const ragDocument = React.useRef<string>("");
    const ragUserInput = React.useRef<string | undefined>(undefined);

    const [chatInputState, setChatInputState] = React.useState<ChatInputState>(ChatInputState.Ready);

    const onDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setIsDraggingOver(true);
    };
    const onDragLeave = (e: React.DragEvent<HTMLDivElement | HTMLTextAreaElement>) => {
        e.preventDefault();
        setIsDraggingOver(false);
    };

    React.useEffect(() => {
        if (!shouldAutoScroll) return;
        scrollViewTargetRef.current?.scrollTo(0, scrollViewTargetRef.current.scrollHeight);
    }, [messages, shouldAutoScroll]);

    React.useEffect(() => {
        const onScroll = () => {
            if (!scrollViewTargetRef.current) return;
            const { scrollTop, scrollHeight, clientHeight } = scrollViewTargetRef.current;
            const isAtBottom = scrollTop + clientHeight >= scrollHeight - 10;
            setShouldAutoScroll(isAtBottom);
        };

        if (!scrollViewTargetRef.current) return;

        const currentScrollViewTarget = scrollViewTargetRef.current;

        currentScrollViewTarget.addEventListener('scroll', onScroll);
        return () => {
            currentScrollViewTarget.removeEventListener('scroll', onScroll);
        };
    }, []);

    React.useEffect(() => {
        if (conversations[selectedId].locked) {
            setChatInputState(ChatInputState.IsLocked);
        } else if (conversations[selectedId].maxTurnReached) {
            setChatInputState(ChatInputState.MaxTurnReached);  
        } else if (!features[FeatureKeys.RagInput].enabled && !isSkippable(conversations[selectedId].botResponseStatus)) {
            setChatInputState(ChatInputState.Sending);
        } else if (chatInputState !== ChatInputState.Sending) {
            setChatInputState(ChatInputState.Ready);
        } else if (!features[FeatureKeys.RagInput].enabled && isSkippable(conversations[selectedId].botResponseStatus)) {
            setChatInputState(ChatInputState.Ready);
        } 

        ragDocument.current = conversations[selectedId].ragDocument;
        ragUserInput.current = conversations[selectedId].ragUserInput;

    }, [conversations, selectedId, chatInputState, features]);

    const handleSubmit = async (options: GetResponseOptions) => {
        setChatInputState(ChatInputState.Sending);
        //If we have the RAG feature enabled, we make a post request to set the document first.
        await chat.getResponse(options);
        setShouldAutoScroll(true);

        //We also need to check if the conversation has reached the maximum number of turns
        if (features[FeatureKeys.RagInput].enabled) {
            if (ragInput.maxTurns > 0 && conversations[selectedId].messages.length + 1 / 2 > ragInput.maxTurns) {
                setChatInputState(ChatInputState.MaxTurnReached);
                dispatch(
                    editConversationMaxTurnReached({
                        id: selectedId,
                        newMaxTurnReached: true,
                    })
                )
            } else {
                setChatInputState(ChatInputState.Ready);
            }
        }
       
    };

    if (conversations[selectedId].hidden) {
        return (
            <div className={classes.root}>
                <div className={classes.scroll}>
                    <div className={classes.history}>
                        <h3>
                            This conversation is not visible in the app because{' '}
                            {Features[FeatureKeys.MultiUserChat].label} is disabled. Please enable the feature in the
                            settings to view the conversation, select a different one, or create a new conversation.
                        </h3>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={classes.root} onDragEnter={onDragEnter} onDragOver={onDragEnter} onDragLeave={onDragLeave}>
            <div ref={scrollViewTargetRef} className={classes.scroll}>
                <div className={classes.history}>
                    <ChatHistory messages={messages} />
                </div>
            </div>
            <div className={classes.input}>
                <ChatInput isDraggingOver={isDraggingOver} onDragLeave={onDragLeave} onSubmit={handleSubmit} chatInputState={chatInputState} />
            </div>
        </div>
    );
};
