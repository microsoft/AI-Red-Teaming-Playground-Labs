// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1, Caption1, Card, CardHeader, makeStyles, Textarea, tokens } from "@fluentui/react-components";
import { FC } from "react";
import { AuthorRoles } from "../../libs/models/ChatMessage";


export const useClasses = makeStyles({
  root: {
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    paddingBottom: tokens.spacingVerticalS,
    paddingTop: tokens.spacingVerticalS,
    borderRadius: tokens.borderRadiusMedium,
    maxWidth: '80%',
    textAlign: 'left',
  },
  message: {
    textWrap: 'wrap',
    overflowWrap: 'break-word',
  },
  user: {
    backgroundColor: tokens.colorBrandStroke2,
    alignSelf: 'flex-end'
  },
  bot: {
    backgroundColor: tokens.colorNeutralBackground1,
    alignSelf: 'flex-start'
  },
  document: {
    marginTop: tokens.spacingVerticalM,
  },
  image: {
    maxWidth: "100%",
    height: "auto"
  }
});

export interface IMessageProps {
  role: AuthorRoles;
  message: string;
  document: string;
  picture: string;
}

export const Message: FC<IMessageProps> = ({ role, message, document, picture }) => {
  const classes = useClasses();
  return (
    <div className={[classes.root,
    role === AuthorRoles.Bot ?
      classes.bot :
      classes.user].join(" ")}>
      <div className={classes.message}>
        {message}
        {picture && (
          <img alt="Review" src={picture} className={classes.image}></img>
        )}
        {document && (
          <div className={classes.document}>
            <Card>
              <CardHeader header={
                <Body1><b>Document</b></Body1>
              }
                description={
                  <Caption1>
                    Below is the document that the chatbot references.
                  </Caption1>
                } />
              <Textarea value={document} disabled resize="vertical"></Textarea>
            </Card>
          </div>
        )}
      </div>
    </div>
  )
};