# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from dataclasses import dataclass
from dataclasses_json import DataClassJsonMixin
from strenum import StrEnum
from typing import Optional

@dataclass
class ChallengeSettingsResponse(DataClassJsonMixin):
    id: int
    name: str
    description: str


class ReviewStatus(StrEnum):
    READY = "ready"
    REVIEWING = "reviewing"
    REVIEWED = "reviewed"

@dataclass
class ScoringResultResponse(DataClassJsonMixin):
    passed: bool
    message: str
    flag: Optional[str]

@dataclass
class SubmissionStatusResponse(DataClassJsonMixin):
# Dataclass used to return the status of the submission
    picture_id: str
    status: ReviewStatus
    scoring_result: Optional[ScoringResultResponse]

@dataclass
class ScoringRequestResponse(DataClassJsonMixin):
# Dataclass received by the scoring service
    passed: bool
    conversation_id: str
    custom_message: str


@dataclass
class ScoringRequest(DataClassJsonMixin):
# Dataclass used to send to the scoring service
    challenge_id: int
    challenge_goal: str
    challenge_title: str
    picture: str
    timestamp: str
    conversation_id: str
    answer_uri: str

@dataclass
class AuthErrorResponse(DataClassJsonMixin):
    auth_type: str
    error: str
    redirect_uri: str
