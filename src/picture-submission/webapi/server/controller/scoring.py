# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import datetime
import requests
import uuid

from flask import Flask, Response
from urllib.parse import urljoin

from server.dtos import ReviewStatus, ScoringRequest, ScoringResultResponse
from server.settings import CONFIG_CHALLENGE_FLAG, CONFIG_CHALLENGE_SETTINGS, CONFIG_CHALLENGE_ID, CONFIG_CHALLENGE_NAME, CONFIG_CHALLENGE_GOAL, CONFIG_SCORING_SETTINGS, CONFIG_SCORING_SCORING_ENDPOINT, CONFIG_SCORING_KEY, CONFIG_SCORING_SUBMISSION_ENDPOINT
from server.models.submission import SubmissionModel
from server.service.ctfd.ctfd import get_ctfd_service_instance

class ScoringController:
    def __init__(self, app: Flask, submission_model: SubmissionModel):
        self.__app = app
        self.__logger = logging.getLogger(__name__)
        self.__submission_model = submission_model
        self.__ctfd_service = get_ctfd_service_instance()

    def set_score(self, id: str, passed: bool, message: str) -> Response:
        self.__logger.info(f"Setting score for submission {id} to {passed} with message: {message}")

        flag = None
        if passed:
            flag = self.__app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_FLAG]

        status = self.__submission_model.get_submission(id)
        if status is None:
            self.__logger.error(f"Failed to find submission {id} and score it")
            return Response("Not Found",status=404)

        status.scoring_result = ScoringResultResponse(passed, message, flag)
        status.status = ReviewStatus.REVIEWED
        self.__submission_model.set_submission(id, status)

        if passed:
            self.__ctfd_service.send_flag(id, flag)

        return Response("Ok",status=200)

    def send_scoring_request(self, id: str, picture: str) -> bool:
        self.__logger.info(f"Sending scoring request for submission {id}")

        # Get challenge settings instead
        challenge_id = self.__app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_ID]
        challenge_title = self.__app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_NAME]
        challenge_goal = self.__app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_GOAL]

        # Get scoring settings
        scoring_endpoint = self.__app.config[CONFIG_SCORING_SETTINGS][CONFIG_SCORING_SCORING_ENDPOINT]
        api_key = self.__app.config[CONFIG_SCORING_SETTINGS][CONFIG_SCORING_KEY]
        submission_endpoint = self.__app.config[CONFIG_SCORING_SETTINGS][CONFIG_SCORING_SUBMISSION_ENDPOINT]

        # Create the submission endpoint url
        submission_endpoint = urljoin(submission_endpoint, f"score/{id}")

        timestamp = datetime.datetime.now(datetime.timezone.utc).isoformat()
        conversation_id = str(uuid.uuid4())
        
        request = ScoringRequest(
            challenge_id,
            challenge_title,
            challenge_goal,
            picture,
            timestamp,
            conversation_id,
            submission_endpoint,
        )

        url = urljoin(scoring_endpoint, "/api/score")
        # Send the request to the scoring endpoint
        response = requests.post(url, json=request.to_dict(), headers={"x-scoring-key": api_key})
        if response.status_code != 200:
            self.__logger.error(f"Failed to send scoring request for submission {id} with status code: {response.status_code}")
            return False
        return True
