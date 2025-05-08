// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1, makeStyles, shorthands, Spinner, Title3, tokens } from "@fluentui/react-components";
import { FC, useEffect, useState } from "react";
import { useChallenge } from "../libs/hooks/useChallenge";
import { ReviewStatus } from "../libs/models/SubmissionStatus";
import { useAppDispatch } from "../redux/app/hooks";
import { setChallengeSettings, setPicture, setStatus } from "../redux/features/app/appSlice";
import { PictureUpload } from "./PictureUpload";

export const useClasses = makeStyles({
  root: {
    width: '100%',
    height: '100%',
    display: 'flex',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
  },
  container: {
    height: '100%',
    width: '100%',
    display: 'flex',
    flexDirection: 'column',
  },
  informativeView: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.padding('80px'),
    alignItems: 'center',
    ...shorthands.gap(tokens.spacingVerticalXL),
    marginTop: tokens.spacingVerticalXXXL,
  },
  errorText: {
    color: tokens.colorStatusDangerForeground1,
  }
});


export const MainView: FC = () => {
  const classes = useClasses();
  const challengeService = useChallenge();
  const dispatch = useAppDispatch();

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        console.log('fetching data');
        const settings = await challengeService.getChallengeSettings();
        const status = await challengeService.getSubmissionStatus();
        
        if (status.status !== ReviewStatus.READY) {
          const picture = await challengeService.getPicture(status.picture_id);
          dispatch(setPicture(URL.createObjectURL(picture)));
        }

        setIsLoading(false);
        dispatch(setStatus(status));
        dispatch(setChallengeSettings(settings));

      } catch (error: any) {
        setErrorMessage(error.message);
        console.error(error);
      }
    };
    fetchData();
    
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <main className={classes.root}>
      <div className={classes.container}>
        {isLoading ?
          <div className={classes.informativeView}>
            <Title3>Loading your challenge</Title3>
            <Spinner />
            {errorMessage && <Body1 className={classes.errorText}>There was an error loading the challenge settings: {errorMessage}</Body1>}
          </div> : <PictureUpload />}
      </div>
    </main>
  )
}