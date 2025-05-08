# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import redis

from typing import Optional
from dataclasses import dataclass
from dataclasses_json import DataClassJsonMixin
from flask import Flask
from azure.storage.blob import BlobServiceClient, ContentSettings

from server.dtos import ReviewStatus, ScoringResultResponse, SubmissionStatusResponse
from server.keys import REDIS_SUBMISSION_KEY
from server.service.ctfd.ticket import CtfdAuthTicket
from server.settings import CONFIG_CHALLENGE_ID, CONFIG_CHALLENGE_SETTINGS, CONFIG_STORAGE_CONTAINER_NAME


@dataclass
class SubmissionStatus(DataClassJsonMixin):
    picture_id: str
    status: ReviewStatus
    scoring_result: Optional[ScoringResultResponse]
    ticket: Optional[CtfdAuthTicket]

    def to_response(self):
        return SubmissionStatusResponse(self.picture_id, self.status, self.scoring_result)

class SubmissionModel:
    def __init__(self, r: redis.Redis, blob_service_client: BlobServiceClient, app: Flask):
        self.__r = r
        self.__blob_service_client = blob_service_client
        self.__container_name = app.config[CONFIG_STORAGE_CONTAINER_NAME]
        self.__challenge_id = app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_ID]

    def get_submission(self, user_id: str) -> Optional[SubmissionStatus]:
        status = self.__r.get(self.__submission_key(user_id))
        if status is None:
            return None
        return SubmissionStatus.from_json(status)

    def set_submission(self, user_id: str, status: SubmissionStatus):
        self.__r.set(self.__submission_key(user_id), status.to_json())

        if status.status == ReviewStatus.REVIEWED:
            # Update the metadata of the picture to include the decision.
            blob_client = self.__blob_service_client.get_blob_client(self.__container_name, self.__blob_key(user_id))
            metadata = blob_client.get_blob_properties().metadata
            metadata["passed"] = str(status.scoring_result.passed)
            metadata["custom_message"] = status.scoring_result.message
            blob_client.set_blob_metadata(metadata)

    def get_picture(self, user_id: str) -> Optional[tuple[bytes, str]]:
        blob_client = self.__blob_service_client.get_blob_client(self.__container_name, self.__blob_key(user_id))
        if not blob_client.exists():
            return None
        
        picture = blob_client.download_blob().readall()
        properties = blob_client.get_blob_properties()
        return picture, properties.content_settings.content_type


    def set_picture(self, user_id: str, picture: bytes, mime: str):
        blob_client = self.__blob_service_client.get_blob_client(self.__container_name, self.__blob_key(user_id))

        content_settings = ContentSettings(content_type=mime)
        blob_client.upload_blob(picture, content_settings=content_settings, metadata={"challenge_id": str(self.__challenge_id)}, overwrite=True)
    
    def __blob_key(self, user_id: str) -> str:
        return f"challenge{self.__challenge_id}/{user_id}.bin"
    
    def __submission_key(self, user_id: str) -> str:
        return f"{REDIS_SUBMISSION_KEY}{self.__challenge_id}.{user_id}"
