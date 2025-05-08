// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { makeStyles } from '@fluentui/react-components';
import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { AuthorRoles, IChatMessage } from '../../../libs/models/ChatMessage';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys, xssState } from '../../../redux/features/app/AppState';
import * as utils from './../../utils/TextUtils';
const useClasses = makeStyles({
    content: {
        wordBreak: 'break-word',
    },
});

interface ChatHistoryTextContentProps {
    message: IChatMessage;
}

export const ChatHistoryTextContent: React.FC<ChatHistoryTextContentProps> = ({ message }) => {
    const classes = useClasses();

    const { features} = useAppSelector((state: RootState) => state.app);
    const content = utils.replaceCitationLinksWithIndices(utils.formatChatTextContent(message.content), message);

    const needsMarkdown = (content: string) => {
        // If the message contains markdown syntax, enable markdown rendering
        if (content.match(/\*\*.+\*\*/g)) {
            return true;
        }
        return false;
    }

    if (features[FeatureKeys.XssVulnerable].enabled && message.authorRole === AuthorRoles.Bot) {
        xssState.current_chat_id = message.chatId;
    }

    return (
        <div className={classes.content}>
            {!(features[FeatureKeys.XssVulnerable].enabled && message.authorRole === AuthorRoles.Bot) ? (
                <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
            ) : (
                needsMarkdown(content) ? (
                    <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
                ) : (
                    <div dangerouslySetInnerHTML={{ __html: content }} />
                )
            )}
        </div>
    );
};
