// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1Strong, Button, makeStyles, Table, TableBody, TableCell, TableCellLayout, TableHeader, TableHeaderCell, TableRow, tokens, Tooltip } from "@fluentui/react-components";
import { AddRegular, Checkmark16Regular, Delete16Regular, DeleteRegular, Dismiss16Regular, EditRegular, PenDismiss16Regular } from "@fluentui/react-icons";
import { ChangeEvent, FC, useCallback, useEffect, useRef, useState } from "react";
import useLocalStorageState, { LocalStorageState } from "use-local-storage-state";
import { ISettings, SETTINGS_KEY_NAME } from "../libs/models/Settings";


export const useClasses = makeStyles({
  actionsColumn: {
    width: '70px',
  },
  actionContainer: {
    display: 'flex',
    width: '100%',
    justifyContent: 'flex-end',
  },
  addReplyButton: {
    //width: '100%',
  },
  buttonContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
    width: '70px'
  },
  edittingContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  edittingButtonContainer: {
    display: 'flex',
  },
  textArea: {
    fontSize: tokens.fontSizeBase300,
    fontFamily: tokens.fontFamilyBase,
    color: tokens.colorNeutralForeground1,
    backgroundColor: 'rgba(0, 0, 0, 0)',
    width: '100%',
    border: 'none',
    "&:focus": {
      outline: 'none',
    }
  },
  cell: {
    whiteSpace: 'pre-wrap',
  }

})

export interface StandardRepliesTableProps {
  onSelectReply?: (reply: string) => void;
}

export const StandardRepliesTable: FC<StandardRepliesTableProps> = ({onSelectReply}) => {
  const classes = useClasses();

  const [settings, setSettings] = useLocalStorageState(SETTINGS_KEY_NAME) as LocalStorageState<ISettings>;
  const [edditingItem, setEditingItem] = useState(-1);
  const [isDeleting, setIsDeleting] = useState(false);
  const [formValue, setFormValue] = useState('');

  const onUnmount = useRef(() => {  });


  const isEditing = edditingItem !== -1;

  const addReply = useCallback(() => {
    //Add a new reply
    const newSettings = { ...settings };
    newSettings.standardReplies.push('');
    setSettings(newSettings);
    setEditingItem(newSettings.standardReplies.length - 1);
    setFormValue('');
  }, [settings, setSettings]);

  const onEditClick = useCallback((event: { currentTarget: { value: any; }; }) => {
    const index = parseInt(event.currentTarget.value)
    setEditingItem(index);
    const value = settings.standardReplies[index];
    setFormValue(value);
  }, [settings.standardReplies]);

  const onDeleteClick = useCallback((event: { currentTarget: { value: any; }; }) => {
    setIsDeleting(true);
    setEditingItem(parseInt(event.currentTarget.value));
  }, []); 
  
  const onCancelConfirmation = useCallback(() => {
    setEditingItem(-1);
    setIsDeleting(false);
  }, []);

  const onConfirmDeletion = useCallback(() => {
    //Delete the item from the settings
    const newSettings = { ...settings };
    newSettings.standardReplies.splice(edditingItem, 1);
    setSettings(newSettings);
    setEditingItem(-1);
    setIsDeleting(false);
  }, [edditingItem, settings, setSettings]);

  const onCancelEdit = useCallback(() => {
    if (formValue === '' || formValue.trim() === '') {
      //We remove the item from the list
      const newSettings = { ...settings };
      newSettings.standardReplies.splice(edditingItem, 1);
      setSettings(newSettings);
    }
    setEditingItem(-1);
    setFormValue('');
  }, [edditingItem, formValue, settings, setSettings]);

  const onSaveEdit = useCallback(() => {
    //Save the edit
    const newSettings = { ...settings };
    if (formValue === '' || formValue.trim() === '') {
      //We remove the item from the list
      newSettings.standardReplies.splice(edditingItem, 1);
    } else {
      newSettings.standardReplies[edditingItem] = formValue;
    }
    setSettings(newSettings);

    setEditingItem(-1);
    setFormValue('');
  }, [edditingItem, formValue, settings, setSettings]);


  const textAreaResize = (element: HTMLTextAreaElement) => {
    element.style.height = "1px";
    element.style.height = (element.scrollHeight) + "px";
  }

  const onFormValueChanged = useCallback((event: ChangeEvent<HTMLTextAreaElement>) => {
    setFormValue(event.target.value);
    textAreaResize(event.target);
  }, []);

  const onFocus = useCallback((event: React.FocusEvent<HTMLTextAreaElement>) => {
    const target = event.target as HTMLTextAreaElement;
    textAreaResize(target);
  }, []);

  const onRowClick = useCallback((event: React.MouseEvent<HTMLTableCellElement>) => {
    if (onSelectReply && !isEditing) {
      console.log("Row clicked");
      const target = event.target as HTMLElement;
      console.log(target);
      const value = target.getAttribute('data-index');
      if (value) {
        const index = parseInt(value);
        onSelectReply(settings.standardReplies[index]);
      }
    }
  }, [settings, isEditing, onSelectReply]);


  const cleanup = useCallback(() => {
    //Find the empty items and remove them
    const newSettings = { ...settings };
    newSettings.standardReplies = newSettings.standardReplies.filter((value) => value !== '' && value.trim() !== '');
    setSettings(newSettings);
  }, [settings, setSettings]);

  onUnmount.current = cleanup;

  useEffect(() => {
    //Cleanup the settings on unmount
    return () => onUnmount.current();
  }, []);

  return (
    <>
      <Table role="grid">
        <TableHeader>
          <TableRow>
            <TableHeaderCell key="standardReply">
              <Body1Strong>
                Replies
              </Body1Strong>
            </TableHeaderCell>
            <TableHeaderCell key="actions" className={classes.actionsColumn}>
              <div className={ classes.actionContainer}>
                <Tooltip content="Add a new reply" relationship="label">
                  <Button appearance="subtle" icon={<AddRegular />} aria-label="Add" disabled={isEditing} onClick={addReply} />
                </Tooltip>
              </div>
            </TableHeaderCell>
          </TableRow>
        </TableHeader>
        <TableBody>
          {settings.standardReplies.map((value, index) => (
            <TableRow key={index}>
              <TableCell data-index={index} onClick={onRowClick} tabIndex={0} role="gridcell" className={classes.cell}>
                {edditingItem === index && !isDeleting ?
                  <div className={classes.edittingContainer}>
                    <textarea
                      className={classes.textArea}
                      placeholder={formValue}
                      value={formValue}
                      onChange={onFormValueChanged}
                      onFocus={onFocus}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' && !event.shiftKey) {
                          event.preventDefault();
                          onSaveEdit();
                        }
                        if (event.key === 'Escape') {
                          event.preventDefault();
                          onCancelEdit();
                        }
                      }}
                      autoFocus
                    />
                    <div>
                      <div className={classes.edittingButtonContainer}>
                        <Tooltip content="Save changes" relationship="label">
                          <Button size="small" appearance="subtle" icon={<Checkmark16Regular />} onClick={onSaveEdit}/>
                        </Tooltip>
                        <Tooltip content="Cancel changes" relationship="label">
                          <Button size="small" appearance="subtle" icon={<PenDismiss16Regular />} onClick={onCancelEdit}/>
                        </Tooltip>
                      </div>
                    </div>
                  </div>
                  :
                  value

                }
              </TableCell>
              <TableCell tabIndex={0} role="gridcell">
                <TableCellLayout>
                  <div className={classes.buttonContainer}>
                    <Tooltip content="Edit reply" relationship="label">
                      <Button value={index} icon={<EditRegular />} aria-label="Edit" disabled={isEditing} onClick={onEditClick}/>
                    </Tooltip>
                    {(edditingItem !== index || !isDeleting) && (
                      <Tooltip content="Delete reply" relationship="label">
                        <Button value={index} icon={<DeleteRegular />} aria-label="Delete" disabled={isEditing} onClick={onDeleteClick} />
                      </Tooltip>
                    )}
                    {isDeleting && edditingItem === index && (
                      <>
                        <Tooltip content="Cancel deletion" relationship="label">
                          <Button appearance="subtle" icon={<Dismiss16Regular />} onClick={onCancelConfirmation} style={{transition:"margin-left 0.3s"}}/>
                        </Tooltip>
                        <Tooltip content="Confirm deletion" relationship="label">
                          <Button appearance="subtle" icon={<Delete16Regular />} onClick={onConfirmDeletion} style={{transition:"margin-left 0.3s"}} />
                        </Tooltip>
                      </>
                    )}

                  </div>
                </TableCellLayout>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </>
  )
}