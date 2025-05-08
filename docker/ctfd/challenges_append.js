// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//Patch to perfom a reload of the page when a challenge is solved
document.addEventListener('DOMContentLoaded', () => {
  setInterval(() => {
    const reloadCmd = localStorage.getItem('reloadCmd');
    if (reloadCmd) {
      localStorage.removeItem('reloadCmd');
      window.location.reload();
    }
  }, 1500);
});
