// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Body1, Body1Strong, Body2, Button, Dialog, DialogActions, DialogBody, DialogContent, DialogSurface, DialogTitle, DialogTrigger, makeStyles, Spinner, Text, tokens, Tooltip } from "@fluentui/react-components";
import { DocumentEdit16Filled } from "@fluentui/react-icons";
import { FC, useEffect, useRef, useState } from "react";
import { useChallenge } from "../libs/hooks/useChallenge";
import { ReviewStatus } from "../libs/models/SubmissionStatus";
import { useAppDispatch, useAppSelector } from "../redux/app/hooks";
import { setStatus } from "../redux/features/app/appSlice";

export const useClasses = makeStyles({
  dragDropArea: {
    width: '100%',
    height: '200px',
    border: '2px dashed #ccc',
    borderRadius: '4px',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalS,
  },
  labelUpload: {
    height: '100%',
    width: '100%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
  },
  buttonCointainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  errorText: {
    color: tokens.colorStatusDangerForeground1
  }
});

const POLLING_INTERVAL = 10000; //Check every 10 seconds if the status has changed

export const PictureUpload: FC = () => {
  const classes = useClasses();
  const challengeService = useChallenge();
  const dispatch = useAppDispatch();


  const { status } = useAppSelector((state) => state.app);
  const { picture } = useAppSelector((state) => state.app);

  const [uploadedImage, setUploadedImage] = useState<string | null>(picture);
  const [file, setFile] = useState<File | null>(null);

  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [isRequestPerformed, setIsRequestPerformed] = useState(false);
  const [clientErrorMessage, setClientErrorMessage] = useState<string | null>(null);

  const inputFileUpload = useRef<HTMLInputElement>(null);

  const handleDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      processFileUpload(file);
    }
  };

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      processFileUpload(file);
    }
  };

    const handlePaste = (event: React.ClipboardEvent<HTMLDivElement>) => {
    event.preventDefault();
    const items = event.clipboardData?.items;
    if (items) {
      for (let i = 0; i < items.length; i++) {
        const item = items[i];
        if (item.type.indexOf("image") !== -1) {
          const file = item.getAsFile();
          if (file) {
            processFileUpload(file);
            break;
          }
        }
      }
    }
  };
  const processFileUpload = (file: File) => {
    if (file.type !== 'image/png' && file.type !== 'image/jpeg' && file.type !== 'image/webp' && file.type !== 'image/gif') {
      setClientErrorMessage("File format not supported. Accepted formats are (png, jpeg, webp, gif)");
      return;
    }
    
    setClientErrorMessage(null);

    const reader = new FileReader();
    reader.onload = () => {
      const imageDataURL = reader.result as string;
      setUploadedImage(imageDataURL);
    };
    reader.readAsDataURL(file);
    if (file.size > 5 * 1024 * 1024) {
      setClientErrorMessage("File size exceeds the maximum limit of 5MB");
      return;
    }       
    setClientErrorMessage(null);
    setFile(file);
  }

  const handleClear = () => {
    setUploadedImage(null);
    inputFileUpload.current!.value = '';
    setClientErrorMessage(null);
    dispatch(setStatus({ ...status, status: ReviewStatus.READY, scoring_result: null }));
  };

  const dialogOpenChangeEvent = (open: boolean) => {
    if (open) {
        //We reset the component if the user did not send a request
        setIsRequestPerformed(false);
    }
    setErrorMessage("");
    setIsDialogOpen(open);
  }

  const sendReview = () => {
    setIsLoading(true);

    //We upload the image to the server
    const sendReviewAsync = async () => {
      try {
        await challengeService.submitPicture(file!);
        setIsLoading(false);
        setErrorMessage("");
        setIsDialogOpen(false);
        setIsRequestPerformed(true);
        dispatch(setStatus({ ...status, status: ReviewStatus.REVIEWING }));        
      }catch (error: any) {
        console.error(error);
        setErrorMessage(error.message);
        setIsLoading(false);
      }
    };

    sendReviewAsync();
  }

  useEffect(() => {
    if (picture) {
      setUploadedImage(picture);
    }
  }, [picture, setUploadedImage]);

  useEffect(() => {
    if (status.status === ReviewStatus.REVIEWING) {
      //We need to poll the server to check if the status has changed
      const interval = setInterval(() => {
        challengeService.getSubmissionStatus().then(newStatus => {
            if (newStatus.status === ReviewStatus.REVIEWED) {
              clearInterval(interval);
              dispatch(setStatus(newStatus));

              if (newStatus.scoring_result?.passed) {
                //We set the refresh cmd in the local storage to refresh the CTFd page
                localStorage.setItem('reloadCmd', 'true');
              }
            }
          }).catch(error => {
            console.error(error);
            setErrorMessage(error.message);
            clearInterval(interval);
          });
      }, POLLING_INTERVAL);
    }
  }, [status.status, challengeService, dispatch]);


  

  return (
    <div>
      <p>
        <Body2>Submit your picture below for review.</Body2><br/>
        <Body1>Accepted formats are (png, jpeg, webp, gif) with a maximum size of 5Mb.</Body1>
      </p>
      <div className={classes.dragDropArea} onDragOver={(event) => event.preventDefault()} onDrop={handleDrop} onPaste={handlePaste}>
        <input id="fileInput" type="file" accept="image/png, image/jpeg, image/webp, image/gif" onChange={handleFileUpload} ref={inputFileUpload} style={{ display: 'none' }} />
        {uploadedImage ? (
          <img src={uploadedImage} alt="Uploaded submission" style={{ maxWidth: '100%', maxHeight: '100%' }} />
        ) : (
          <label htmlFor="fileInput" className={classes.labelUpload}>
            <Body1>Drag and drop / paste (crtl + v) / click to browse to upload a picture</Body1>
          </label>
        )}
      </div>
      {clientErrorMessage && (<Body1 className={classes.errorText}>{clientErrorMessage}</Body1>)}
      {uploadedImage && (
        <>
          <div className={classes.buttonCointainer}>
            <Dialog
              modalType='alert'
              open={isDialogOpen}
              onOpenChange={(_, data) => { dialogOpenChangeEvent(data.open) }}
            >

              <DialogTrigger disableButtonEnhancement>
                <Tooltip content="Send the picture for manual review" relationship={"label"}>
                  <Button appearance="primary" icon={<DocumentEdit16Filled />} disabled={clientErrorMessage !== null || status.status !== ReviewStatus.READY}>Send for review</Button>
                </Tooltip>
              </DialogTrigger>
              
              <DialogSurface>
                <DialogBody>
                  <DialogTitle>Confirm Review</DialogTitle>
                  <DialogContent>
                    {(isLoading || (!isDialogOpen && isRequestPerformed)) && (
                      <Spinner label="Sending review..." size='tiny' />
                    )}
                    {!isLoading && (
                      <>
                        {errorMessage === "" && !isRequestPerformed && (
                          <>
                            A human reviewer will check your picture. Please be mindful of the reviewer&apos;s workload and only ask for a review when you have completed the challenge goals.
                          </>
                        )}
                        {errorMessage !== "" && (
                          <>
                            <Text size={200}>
                              {errorMessage}
                            </Text>
                          </>
                        )}
                      </>
                    )}
                  </DialogContent>
                  <DialogActions>
                    <DialogTrigger disableButtonEnhancement>
                      <Button appearance="secondary" disabled={isLoading || !isDialogOpen}>Cancel</Button>
                    </DialogTrigger>
                    <Button appearance="primary" disabled={isLoading || !isDialogOpen} onClick={sendReview}>Proceed</Button>
                  </DialogActions>
                </DialogBody>
              </DialogSurface>
            </Dialog>
            <Button onClick={handleClear} disabled={status.status === ReviewStatus.REVIEWING || status.status === ReviewStatus.REVIEWED && status.scoring_result?.passed}>Clear</Button>
          </div>
          {status.status === ReviewStatus.REVIEWING && (
              <div>
                <Spinner label="Reviewing your submission..." size="medium" />
              </div>
          )}
          {status.status === ReviewStatus.REVIEWED && (
            <div>
              <p>
                <Body1>{status.scoring_result?.passed ?
                  <><Body1Strong>Congratulations! </Body1Strong>You passed the challenge. Here's the flag that was already submited on your behalf: {status.scoring_result.flag}</> 
                  : "Unfortunately, you did not pass the challenge."}</Body1><br/>
                {status.scoring_result?.message && (<><Body1Strong>Message from scorer</Body1Strong><Body1>: {status.scoring_result?.message}</Body1></>)}
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}