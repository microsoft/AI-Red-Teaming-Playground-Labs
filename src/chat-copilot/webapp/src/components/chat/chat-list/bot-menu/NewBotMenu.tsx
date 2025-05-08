// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FC, useState } from 'react';

import { Button, Menu, MenuItem, MenuList, MenuPopover, MenuTrigger, Tooltip } from '@fluentui/react-components';
import { BotAdd20Regular } from '@fluentui/react-icons';
import { useChat } from '../../../../libs/hooks';
import { BotAdd20 } from '../../../shared/BundledIcons';
import { InvitationJoinDialog } from '../../invitation-dialog/InvitationJoinDialog';


export const NewBotMenu: FC = () => {
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
            <Menu>
                <MenuTrigger disableButtonEnhancement>
                    <Tooltip content="Create new conversation" relationship="label">
                        <Button
                            data-testid="createNewConversationButton"
                            icon={<BotAdd20 />}
                            appearance="transparent"
                        />
                    </Tooltip>
                </MenuTrigger>
                <MenuPopover>
                    <MenuList>
                        <MenuItem data-testid="addNewBotMenuItem" icon={<BotAdd20Regular />} onClick={onAddChat}>
                            Add a new Bot
                        </MenuItem>
                    </MenuList>
                </MenuPopover>
            </Menu>
            {isJoiningBot && <InvitationJoinDialog onCloseDialog={onCloseDialog} />}
        </div>
    );
};
