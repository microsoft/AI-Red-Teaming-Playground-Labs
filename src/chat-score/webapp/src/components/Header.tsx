// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body2, makeStyles, Title3, tokens } from "@fluentui/react-components";
import { FC } from "react";
import { useAppSelector } from "../redux/app/hooks";
import { RootState } from "../redux/app/store";
import { SettingsDialog } from "./SettingsDialog";

export const useClasses = makeStyles({
  root: {
    paddingLeft: tokens.spacingHorizontalL,
    paddingRight: tokens.spacingHorizontalL,
    height: '45px',
    backgroundColor: tokens.colorBrandForeground1,
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

  return (
    <header className={classes.root}>
      <Title3>Conversation Scorer</Title3>
      <div>
        {appState.connected ?
          <Body2>{appState.connectionCount} connected scorer(s)</Body2> :
          <Body2>Connecting...</Body2>
        }
        <SettingsDialog/>
      </div>
    </header>
  );
};