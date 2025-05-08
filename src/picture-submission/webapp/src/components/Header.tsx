// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button, makeStyles, Title3, tokens, Tooltip } from "@fluentui/react-components";
import { Settings24Regular } from "@fluentui/react-icons";
import { FC, useEffect, useState } from "react";
import { useAppSelector } from "../redux/app/hooks";
import { RootState } from "../redux/app/store";
import { DescriptionDialog } from "./DescriptionDialog";

export const useClasses = makeStyles({
  root: {
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    height: '48px',
    backgroundColor: tokens.colorBrandForeground2,
    boxShadow: tokens.shadow4Brand,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    color: tokens.colorNeutralForegroundOnBrand,
    position: "relative",
    zIndex: 10,
  }
});

export const Header: FC = () => {
  const classes = useClasses();

  const appState = useAppSelector((state: RootState) => state.app);
  const challengeSettings = appState.challengeSettings;

  const [showDescription, setShowDescription] = useState(false);

  useEffect(() => {
    const key = `dontShowAgain-${challengeSettings.id}`;
    const result = localStorage.getItem(key);
    if (result && result === 'true') {
      setShowDescription(false);
    } else if (challengeSettings.description !== "") {
      setShowDescription(true);
    }

    //Update the title of webpage
    if (document.title !== challengeSettings.name && challengeSettings.name !== "") {
        document.title = challengeSettings.name + " - Picture Submission";
    }
  }, [challengeSettings]);
  
  return (
    <header className={classes.root}>
        <Title3>{appState.challengeSettings.name}</Title3>
        <DescriptionDialog description={appState.challengeSettings.description} open={showDescription} />
        <Tooltip content="There are no settings for this challenge" relationship={"label"} >
          <Button
            disabled
            appearance="transparent"
            icon={<Settings24Regular color={tokens.colorNeutralForegroundDisabled} />}>
            Settings
          </Button>
        </Tooltip>
    </header>
  );
}