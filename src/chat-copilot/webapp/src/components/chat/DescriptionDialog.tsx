// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  makeStyles,
  shorthands,
  tokens
} from '@fluentui/react-components';
import { QuestionCircle24Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { useAppSelector } from '../../redux/app/hooks';
import { SharedStyles } from '../../styles';


const useClasses = makeStyles({
  root: {
    ...shorthands.overflow('hidden'),
    display: 'flex',
    flexDirection: 'column',
    height: '600px',
  },
  outer: {
    paddingRight: tokens.spacingVerticalXS,
  },
  content: {
    height: '100%',
    ...SharedStyles.scroll,
    paddingRight: tokens.spacingVerticalL,
  },
  footer: {
    paddingTop: tokens.spacingVerticalL,
  },
  titlebutton: {
    cursor: 'pointer',
  }
});

interface DescriptionDialogProps {
  description: string;
  open: boolean;
}

export const DescriptionDialog: React.FC<DescriptionDialogProps> = ({
  description,
  open
}) => {
  const classes = useClasses();

  const { challengeSettings } = useAppSelector((state) => state.app);
  const [openInternal, setOpenInternal] = useState(open);
  const [dontShowAgain, setDontShowAgain] = useState(false);

  React.useEffect(() => {
    setOpenInternal(open);
  }, [open])

  React.useEffect(() => {
    const key = `dontShowAgain-${challengeSettings.id}`;
    const result = localStorage.getItem(key)
    if (result && result === 'true') {
      setDontShowAgain(true);
    } else {
      setDontShowAgain(false);
    }
  }, [challengeSettings]);

  return (
    <Dialog
      open={openInternal}
      onOpenChange={(_event, data) => {
        setOpenInternal(data.open);
      }}
    >
      <DialogTrigger>
        <Button
          style={{ color: 'white' }}
          title='Objectives'
          icon={<QuestionCircle24Regular color="white" />}
          appearance='transparent'
        >Objectives</Button>
      </DialogTrigger>
      <DialogSurface className={classes.outer}>
        <DialogBody className={classes.root}>
          <DialogTitle>
            Challenge Description
          </DialogTitle>
          <DialogContent className={classes.content}>
            <div>
              <ReactMarkdown remarkPlugins={[remarkGfm]}>{description}</ReactMarkdown>
            </div>
          </DialogContent>
        </DialogBody>
        <DialogActions position="start" className={classes.footer}>
          <DialogTrigger disableButtonEnhancement>
            <Button appearance="secondary">
              Let&apos;s start!
            </Button>
          </DialogTrigger>
          <Checkbox label="Don't show this again" checked={dontShowAgain} onChange={(_, data) => {
            const key = `dontShowAgain-${challengeSettings.id}`;
            localStorage.setItem(key, data.checked ? 'true' : 'false');
            setDontShowAgain(Boolean(data.checked));
          }}/>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};