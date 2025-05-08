// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1, Divider, makeStyles, Skeleton, SkeletonItem, tokens } from "@fluentui/react-components";
import { FC } from "react";
import { useAppSelector } from "../../redux/app/hooks";
import { ConversationView } from "./ConversationView";
import { ScoringPane } from "./ScoringPane";

export const useClasses = makeStyles({
  root: {
    width: '100%',
    height: '100%',
    display: 'flex',
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingLeft: tokens.spacingHorizontalM,
  },
  conversationReviewPane: {
    height: '100%',
    width: '100%',
    display: 'flex',
    flexDirection: 'column',
  },
  skeleton: {
    width: '15em',
    display: "inline-block",
    verticalAlign: "text-bottom",
  },
  label: {
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
  }
});


export const MainView: FC = () => {
  const classes = useClasses();

  const conversationReview = useAppSelector((state) => state.app.conversationReview);

  return (
    <main className={classes.root}>
      <div className={classes.conversationReviewPane}>
        <div className='challenge-details'>
          {conversationReview && (
            <>
              <div className={classes.label}><Body1>Title: {conversationReview?.title}</Body1></div>
              <div className={classes.label}><Body1>Goal: {conversationReview?.goal}</Body1></div>
              <div className={classes.label}><Body1>Id: {conversationReview?.id}</Body1></div>
            </>
          )}
          {!conversationReview && (
            <>
              <div className={classes.label}>
                <Body1>Title: <Skeleton aria-label="Loading content" className={classes.skeleton}><SkeletonItem /></Skeleton></Body1>
              </div>
              <div className={classes.label}>
                <Body1>Goal: <Skeleton aria-label="Loading content" className={classes.skeleton}><SkeletonItem /></Skeleton></Body1>
              </div>
              <div className={classes.label}>
                <Body1>Id: <Skeleton aria-label="Loading content" className={classes.skeleton}><SkeletonItem /></Skeleton></Body1>
              </div>
            </>
          )}
          <Divider></Divider>
        </div>
        <ConversationView></ConversationView>
      </div>
      <ScoringPane></ScoringPane>
    </main>
  )
};