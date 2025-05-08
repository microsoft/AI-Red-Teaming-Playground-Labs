// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle, DialogTrigger, Spinner, Text, Tooltip, makeStyles } from '@fluentui/react-components';
import { DocumentEdit16Filled } from '@fluentui/react-icons';
import React, { useState } from 'react';
import { useScoring } from '../../../libs/hooks/useScoring';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        'place-content': 'flex-end',
        alignItems: 'center',
    },
});

interface IScoringProps {
    messageIndex: number;
    chatId: string;
}

export const ScorerAction: React.FC<IScoringProps> = ({ chatId, messageIndex }) => {
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    
    const classes = useClasses();
    const scoring = useScoring();

    const [isLoading, setIsLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");
    const [isRequestPerformed, setIsRequestPerformed] = useState(false);
    const [isReady, setIsReady] = useState(true);

    const [isDialogOpen, setIsDialogOpen] = useState(false);

    const sendReview = () => {
        setIsLoading(true);
        scoring.createManualScoring(chatId, messageIndex)
        .then(() => {
            setIsLoading(false);
            setErrorMessage("");
            setIsDialogOpen(false);
            setIsRequestPerformed(true);
        }).catch((error: Error) => {
            setErrorMessage(error.message);
            setIsLoading(false);
        });

    };

    React.useEffect(() => {
        const status = conversations[selectedId].botResponseStatus;
        setIsReady(status === "" || !status);
    }, [conversations, selectedId]);

    const dialogOpenChangeEvent = (open: boolean) => {
        if (open) {
            //We reset the component if the user did not send a request
            setErrorMessage("");
            setIsRequestPerformed(false);
        }
        setIsDialogOpen(open);
    }

    return (
        <div className={classes.root}>
            <Text color="gray" size={200}>
                Evaluate answer
            </Text>
            <Dialog
                modalType='alert'
                open={isDialogOpen}
                onOpenChange={(_, data) => { dialogOpenChangeEvent(data.open) }}
            >
                <DialogTrigger disableButtonEnhancement>
                    <Tooltip content={isReady ? "Send answer for manual review": "Wait for text generation to complete"} relationship={'label'}>
                        <Button
                            icon={<DocumentEdit16Filled />}
                            appearance="transparent"
                            aria-label="Edit"
                            disabled={!isReady}
                        />
                    </Tooltip>

                </DialogTrigger>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>Confirm Review</DialogTitle>
                        <DialogContent>
                            {(isLoading || (!isDialogOpen && isRequestPerformed)) && (
                                <Spinner label="Sending review..." size='tiny' />
                            )}
                            
                            {!isLoading && (
                                <> 
                                    {errorMessage === "" && !isRequestPerformed && (
                                        <>
                                            A human reviewer will check your chat conversation. Please be mindful of the reviewer&apos;s workload and only ask for a review when you have completed the challenge goals.
                                        </>
                                    )}
                                    {errorMessage !== "" && (
                                        <>
                                            <Text size={200}>
                                                {errorMessage}
                                            </Text>
                                        </>
                                    )}
                                </>
                            )}
                        </DialogContent>
                        <DialogActions>
                            <DialogTrigger disableButtonEnhancement>
                            <Button appearance="secondary" disabled={isLoading || !isDialogOpen}>Cancel</Button>
                            </DialogTrigger>
                            <Button appearance="primary" disabled={isLoading || !isDialogOpen} onClick={() => { sendReview()}}>Proceed</Button>
                        </DialogActions>
                        </DialogBody>
                </DialogSurface>
            </Dialog>
        </div>
    );
};
