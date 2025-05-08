// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
    AvatarProps,
    Persona,
    Text,
    ToggleButton,
    makeStyles,
    mergeClasses,
    shorthands,
} from '@fluentui/react-components';
import { ChevronDown20Regular, ChevronUp20Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import { Constants } from '../../../Constants';
import { useChat } from '../../../libs/hooks/useChat';
import { AuthorRoles, ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { DefaultChatUser, FeatureKeys } from '../../../redux/features/app/AppState';
import { Breakpoints, customTokens } from '../../../styles';
import { timestampToDateString } from '../../utils/TextUtils';
import { PlanViewer } from '../plan-viewer/PlanViewer';
import { PromptDialog } from '../prompt-dialog/PromptDialog';
import { TypingIndicator } from '../typing-indicator/TypingIndicator';
import * as utils from './../../utils/TextUtils';
import { ChatHistoryDocumentContent } from './ChatHistoryDocumentContent';
import { ChatHistoryTextContent } from './ChatHistoryTextContent';
import { CitationCards } from './CitationCards';
import { RagDocument } from './RagDocument';
import { ScorerAction } from './ScorerAction';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
        maxWidth: '75%',
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        ...Breakpoints.small({
            maxWidth: '100%',
        }),
        ...shorthands.gap(customTokens.spacingHorizontalXS),
    },
    debug: {
        position: 'absolute',
        top: '-4px',
        right: '-4px',
    },
    alignEnd: {
        alignSelf: 'flex-end',
    },
    persona: {
        paddingTop: customTokens.spacingVerticalS,
    },
    item: {
        backgroundColor: customTokens.colorNeutralBackground1,
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        ...shorthands.padding(customTokens.spacingVerticalS, customTokens.spacingHorizontalL),
    },
    me: {
        backgroundColor: customTokens.colorMeBackground,
    },
    time: {
        color: customTokens.colorNeutralForeground3,
        fontSize: customTokens.fontSizeBase200,
        fontWeight: 400,
    },
    header: {
        position: 'relative',
        display: 'flex',
        flexDirection: 'row',
        ...shorthands.gap(customTokens.spacingHorizontalL),
    },
    canvas: {
        width: '100%',
        textAlign: 'center',
    },
    image: {
        maxWidth: '250px',
    },
    blur: {
        filter: 'blur(5px)',
    },
    controls: {
        display: 'flex',
        flexDirection: 'row',
        marginTop: customTokens.spacingVerticalS,
        marginBottom: customTokens.spacingVerticalS,
        ...shorthands.gap(customTokens.spacingHorizontalL),
    },
    citationButton: {
        marginRight: 'auto',
    },
    rlhf: {
        marginLeft: 'auto',
    },
    fullWidth: {
        width: '100%',
    },
});

interface ChatHistoryItemProps {
    message: IChatMessage;
    messageIndex: number;
}

export const ChatHistoryItem: React.FC<ChatHistoryItemProps> = ({ message, messageIndex }) => {
    const classes = useClasses();
    const chat = useChat();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);
    const [showCitationCards, setShowCitationCards] = useState(false);

    const isDefaultUser = message.userName === DefaultChatUser.fullName;
    const isMe = isDefaultUser || (message.authorRole === AuthorRoles.User && message.userId === activeUserInfo?.id);
    const isBot = message.authorRole === AuthorRoles.Bot;
    const user = isDefaultUser
        ? DefaultChatUser
        : chat.getChatUserById(message.userName, selectedId, conversations[selectedId].users);
    const fullName = user?.fullName ?? message.userName;
    const isScoringMessage = ((message: IChatMessage) => {
        return message.content.includes(Constants.scoring.manual_scoring) || message.content.includes(Constants.scoring.auto_scoring)
    })

    const avatar: AvatarProps = isBot
        ? { image: { src: conversations[selectedId].botProfilePicture } }
        : isDefaultUser
            ? { idForColor: selectedId, color: 'colorful' }
            : { name: fullName, color: 'colorful' };

    let content: JSX.Element;
    if (isBot && message.type === ChatMessageType.Plan) {
        content = <PlanViewer message={message} messageIndex={messageIndex} />;
    } else if (message.type === ChatMessageType.Document) {
        content = <ChatHistoryDocumentContent isMe={isMe} message={message} />;
    } else {
        content =
            isBot && message.content.length === 0 ? <TypingIndicator /> : <ChatHistoryTextContent message={message} />;
    }

    const getClassesBubble = () => {
        if (isMe) {
            return mergeClasses(classes.item, classes.me);
        } else {
            if (features[FeatureKeys.RagInput].enabled && messageIndex == 0) {
                return mergeClasses(classes.item, classes.fullWidth);
            }
            return classes.item;
        }
    }


    // TODO: [Issue #42] Persistent RLHF, hook up to model
    // Currently for demonstration purposes only, no feedback is actually sent to kernel / model
    /*const showShowRLHFMessage =
        features[FeatureKeys.RLHF].enabled &&
        message.userFeedback === UserFeedback.Requested &&
        messageIndex === conversations[selectedId].messages.length - 1 &&
        message.userId === 'Bot';
*/
    return (
        <div
            className={isMe ? mergeClasses(classes.root, classes.alignEnd) : classes.root}
            // The following data attributes are needed for CI and testing
            data-testid={`chat-history-item-${messageIndex}`}
            data-username={fullName}
            data-content={utils.formatChatTextContent(message.content)}
        >
            {
                <Persona
                    className={classes.persona}
                    avatar={avatar}
                    presence={
                        !features[FeatureKeys.SimplifiedExperience].enabled && !isMe
                            ? { status: 'available' }
                            : undefined
                    }
                />
            }
            <div className={getClassesBubble()}>
                <div className={classes.header}>
                    {!isMe && <Text weight="semibold">{fullName}</Text>}
                    <Text className={classes.time}>{timestampToDateString(message.timestamp, true)}</Text>
                    {isBot && <PromptDialog message={message} />}
                </div>
                {content}

                {features[FeatureKeys.RagInput].enabled && messageIndex == 0 && (
                    <RagDocument />
                )}

                <div className={classes.controls}>
                    {message.citations && message.citations.length > 0 && (
                        <ToggleButton
                            appearance="subtle"
                            checked={showCitationCards}
                            className={classes.citationButton}
                            icon={showCitationCards ? <ChevronUp20Regular /> : <ChevronDown20Regular />}
                            iconPosition="after"
                            onClick={() => {
                                setShowCitationCards(!showCitationCards);
                            }}
                            size="small"
                        >
                            {`${message.citations.length} ${message.citations.length === 1 ? 'citation' : 'citations'}`}
                        </ToggleButton>
                    )}

                    {features[FeatureKeys.HasHumanScorerOnly].enabled && message.authorRole === AuthorRoles.Bot &&
                        message.type === ChatMessageType.Message &&
                        messageIndex > 0 &&
                        !isScoringMessage(message) && (
                            <div className={classes.rlhf}>{<ScorerAction chatId={message.chatId} messageIndex={messageIndex} />}</div>
                        )}
                </div>
                {showCitationCards && <CitationCards message={message} />}
            </div>
        </div>
    );
};
