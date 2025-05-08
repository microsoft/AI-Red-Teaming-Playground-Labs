// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export const ACTIVITY_THRESHOLD = 5; //If user is inactive for 5 seconds, we consider them inactive

let userInteracted = false;
let lastInteractionTime = Date.now();

export const hasInteracted = () => userInteracted;
export const isUserActive = () => lastInteractionTime + (ACTIVITY_THRESHOLD * 1000) >= Date.now();

export const setupInteractionDetector = () => {
  const soundInteractionEvents = new Set(["click", "keydown", "touchstart"]);
  
  const interactionHandler = (event: Event) => {
    if (!userInteracted && soundInteractionEvents.has(event.type)) {
      userInteracted = true;
      console.log("User interacted");
    }
    lastInteractionTime = Date.now();
  };

  document.addEventListener('click', interactionHandler);
  document.addEventListener('keydown', interactionHandler);
  document.addEventListener('touchstart', interactionHandler);
  document.addEventListener('mousemove', interactionHandler);
  document.addEventListener('scroll', interactionHandler);
  document.addEventListener('wheel', interactionHandler);
};