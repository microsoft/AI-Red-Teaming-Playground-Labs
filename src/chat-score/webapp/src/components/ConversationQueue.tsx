// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Caption1, makeStyles, Skeleton, SkeletonItem, Subtitle1, Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow, tokens } from "@fluentui/react-components";
import { DocumentSignature20Regular, DocumentTextClock20Regular } from "@fluentui/react-icons";
import { FC } from "react";
import { useAppSelector } from "../redux/app/hooks";
import { RootState } from "../redux/app/store";

export const useClasses = makeStyles({
  root: {
    width: '350px',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground5,
    boxShadow: tokens.shadow4,
    display: 'flex',
  },
  container: {
    flexGrow: 1,
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingHorizontalM,
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
  },
  table: {
    display: 'block',
    overflow: 'auto',
  },
  tableId: {
    width: '15%'
  },
  tableChallenge: {
    width: '25%'
  },
  tableStatus: {
    width: '60%'
  },
  caption: {
    marginTop: tokens.spacingVerticalM,
    textAlign: 'center',

  }
});

const NUMBER_SKELTONS = 3;

export const ConversationQueue: FC = () => {
  const classes = useClasses();
  const appState = useAppSelector((state: RootState) => state.app);

  return (
    <aside className={classes.root}>
      <div className={classes.container}>
        <Subtitle1 className={classes.title}>Conversations Queue</Subtitle1>
        <Table size='small' className={classes.table}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell className={classes.tableId}>Id</TableHeaderCell>
              <TableHeaderCell className={classes.tableChallenge}>Challenge</TableHeaderCell>
              <TableHeaderCell className={classes.tableStatus}>Status</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>

            {appState.connected && appState.conversationQueue.map((row, index) => (
              <TableRow key={index}>
                <TableCell>{row.id}</TableCell>
                <TableCell>#{row.challenge_id}</TableCell>
                <TableCell>
                  <TableCellLayout media={row.in_review ? <DocumentSignature20Regular></DocumentSignature20Regular> : <DocumentTextClock20Regular></DocumentTextClock20Regular>}>
                    {row.in_review ? "In Review" : "Awaiting"}
                  </TableCellLayout>
                </TableCell>
              </TableRow>
            ))}
            {(!appState.connected || (appState.connected && appState.conversationQueue.length === 0)) && (
              <>
                {[...Array(NUMBER_SKELTONS)].map((_, index) => (
                  <TableRow key={index}>
                    <TableCell>
                      <Skeleton aria-label="Loading Content">
                        <SkeletonItem />
                      </Skeleton>
                    </TableCell>
                    <TableCell>
                      <Skeleton aria-label="Loading Content">
                        <SkeletonItem />
                      </Skeleton>
                    </TableCell>
                    <TableCell>
                      <Skeleton aria-label="Loading Content">
                        <SkeletonItem />
                      </Skeleton>
                    </TableCell>
                  </TableRow>
                ))}
              </>)}

          </TableBody>
        </Table>
        {(appState.connected && appState.conversationQueue.length === 0) &&
          <div className={classes.caption}>
            <Caption1>Waiting for conversations...</Caption1>
          </div>
          }
      </div>
    </aside>
  );
};