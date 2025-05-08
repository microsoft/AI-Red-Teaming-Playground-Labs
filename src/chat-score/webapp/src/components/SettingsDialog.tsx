// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Accordion, AccordionHeader, AccordionItem, AccordionPanel, Body1, Button, Dialog, DialogBody, DialogContent, DialogSurface, DialogTitle, DialogTrigger, Divider, makeStyles, Switch, tokens } from "@fluentui/react-components";
import { Dismiss24Regular, SettingsRegular } from "@fluentui/react-icons";
import { FC, useCallback } from "react";
import useLocalStorageState, { LocalStorageState } from "use-local-storage-state";
import { ISettings, SETTINGS_KEY_NAME } from "../libs/models/Settings";
import { StandardRepliesTable } from "./StandardRepliesTable";


export const useClasses = makeStyles({
  settingsButton: {
    color: tokens.colorNeutralForegroundOnBrand,
    "&:hover": {
      color: tokens.colorNeutralBackground3Hover
    },
    "&:hover:active": {
      color: tokens.colorNeutralBackground3Pressed
    }
  },
  textSpacing: {
    marginBottom: tokens.spacingVerticalL,
  }
});

export const SettingsDialog: FC = () => {
  const classes = useClasses();
  
  const [settings, setSettings] = useLocalStorageState(SETTINGS_KEY_NAME) as LocalStorageState<ISettings>;


  const onSoundChange = useCallback((event: { currentTarget: { checked: any; }; }) => {
    setSettings({ ...settings, enableSound: event.currentTarget.checked });
  },
    [settings, setSettings]
  );

  const onDarkModeChange = useCallback((event: { currentTarget: { checked: any; }; }) => {
    setSettings({ ...settings, darkMode: event.currentTarget.checked });
  },
    [settings, setSettings]
  );

  return (
    <Dialog modalType="alert">
      <DialogTrigger disableButtonEnhancement>
        <Button className={classes.settingsButton} appearance="transparent" icon={<SettingsRegular/>}></Button>
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle
            action={
              <DialogTrigger action="close">
                <Button appearance="subtle" aria-label="close" icon={<Dismiss24Regular/>} />
              </DialogTrigger>
            }
          >Settings</DialogTitle>
          <DialogContent>
            <Accordion collapsible multiple defaultOpenItems={['basic', 'replies']}>
              <AccordionItem value="basic">
                <AccordionHeader expandIconPosition="end">
                  <h3>Basic</h3>
                </AccordionHeader>
                <AccordionPanel>
                  <Switch label="Sound notifications" checked={ settings.enableSound} onChange={onSoundChange} />
                  <br/>
                  <Switch label="Dark mode" checked={settings.darkMode} onChange={onDarkModeChange}/>
                </AccordionPanel>
              </AccordionItem>
              <Divider />
              <AccordionItem value="replies">
                <AccordionHeader expandIconPosition="end">
                  <h3>Standard Replies</h3>
                </AccordionHeader>
                <AccordionPanel>
                  <Body1 className={classes.textSpacing}>Standard replies are predefined messages that can be used to quickly respond to a user. Use the table below to alter the standard replies. These standard replies are saved in your browser and are not shared with the team.</Body1>
                  <StandardRepliesTable/>
                </AccordionPanel>
              </AccordionItem>
            </Accordion>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}