// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1, Caption1, Card, CardHeader, makeStyles, shorthands, Textarea, tokens } from "@fluentui/react-components";
import { useAppSelector } from "../../../redux/app/hooks";
import { RootState } from "../../../redux/app/store";

const useClasses = makeStyles({
    root: {
        ...shorthands.margin(tokens.spacingVerticalM, 0),
    }
});

export const RagDocument: React.FC = () => {
    const classes = useClasses();
    const ragInput = useAppSelector((state: RootState) => state.app.challengeSettings.ragInput)
    const { conversations, selectedId, } = useAppSelector((state: RootState) => state.conversations);


    return (
        <div className={classes.root}>
            {ragInput.enabled && (
                <>
                    <Card>
                        <CardHeader
                            header={
                                <Body1><b>{ragInput.titleLong}</b></Body1>
                            }
                            description={
                                <Caption1>
                                    Below is the document that the chatbot references.
                                </Caption1>
                            }
                        />
                        <Textarea
                            disabled={true}
                            value={conversations[selectedId].ragDocument}
                            resize="vertical"
                        ></Textarea>
                    </Card>
                </>
            )}
        </div>
    )
};