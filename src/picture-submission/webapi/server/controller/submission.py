# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import uuid
from flask import Response, jsonify, request, Flask
from base64 import b64encode
from magic import Magic

from server.dtos import SubmissionStatusResponse, ReviewStatus
from server.models.submission import SubmissionModel, SubmissionStatus
from server.controller.scoring import ScoringController
from server.service.ctfd.ticket import CtfdAuthTicket

APPROVED_MIME_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"]

class SubmissionController:
    def __init__(self, app: Flask, submission_model: SubmissionModel, scoring_controller: ScoringController):
        self.__submission_model = submission_model
        self.__scoring_controller = scoring_controller

        self.__app = app
        self.__logging = logging.getLogger(__name__)
        self.__mime_detector = Magic(mime=True)

    def get_status(self, user_id: str) -> Response:
        response = None
        status = self.__submission_model.get_submission(user_id)
        if status is None:
            null_guid = uuid.UUID(int=0)
            response = SubmissionStatusResponse(str(null_guid), ReviewStatus.READY, None)
        else:
            response = status.to_response()

        return jsonify(response.to_dict())
    
    def get_picture(self, user_id: str) -> Response:
        # Get the picture bytes from the submission model
        picture_result = self.__submission_model.get_picture(user_id)
        if picture_result is None:
            return Response("Picture not found!",status=404)
        
        picture_bytes, mime = picture_result
        return Response(picture_bytes, status=200, mimetype=mime, headers={"Cache-Control": "max-age=3600"})
    
    def upload_picture(self, ticket: CtfdAuthTicket) -> Response:
        if "file" not in request.files:
            return Response("No file uploaded!",status=400)
        
        file = request.files["file"]
        
        # Check if the size of the file is greater than 5MB
        if file.content_length > 5 * 1024 * 1024:
            return Response("File size too large!",status=400)
        
        picture = file.read() # type: bytes

        # Check if the file is an image
        mime = self.__mime_detector.from_buffer(picture)
        if mime not in APPROVED_MIME_TYPES:
            return Response("Invalid file type!", status=400)

        self.__logging.info(f"Uploaded picture with length: {len(picture)} bytes with a MIME type: {mime}")

        user_id = ticket.id
        self.__submission_model.set_picture(user_id, picture, mime)
        submission = SubmissionStatus(str(uuid.uuid4()), ReviewStatus.REVIEWING, None, ticket)
        self.__submission_model.set_submission(user_id, submission)

        # Send the scoring request
        picture_b64 = b64encode(picture).decode('utf-8')
        picture_src = f"data:{mime};base64,{picture_b64}"
        if self.__scoring_controller.send_scoring_request(user_id, picture_src):
            return Response(status=202)

        return Response("Failed to send the scoring request.",status=500)