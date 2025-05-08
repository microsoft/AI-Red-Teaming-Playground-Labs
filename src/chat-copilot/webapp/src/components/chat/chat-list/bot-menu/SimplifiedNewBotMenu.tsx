// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FC, useState } from 'react';

import {
    Button,
    Tooltip
} from '@fluentui/react-components';
import { useChat } from '../../../../libs/hooks';
import { Add20 } from '../../../shared/BundledIcons';
import { InvitationJoinDialog } from '../../invitation-dialog/InvitationJoinDialog';

export const SimplifiedNewBotMenu: FC = () => {
    const chat = useChat();

    // It needs to keep the menu open to keep the FileUploader reference
    // when the file uploader is clicked.
    const [isJoiningBot, setIsJoiningBot] = useState(false);

    const onAddChat = () => {
        void chat.createChat();
    };

    const onCloseDialog = () => {
        setIsJoiningBot(false);
    };

    return (
        <div>
            <Tooltip content="Add a chat" relationship="label">
                <Button data-testid="createNewConversationButton" icon={<Add20 />} appearance="transparent" onClick={onAddChat} />
            </Tooltip>
            {isJoiningBot && <InvitationJoinDialog onCloseDialog={onCloseDialog} />}
        </div>
    );
};
