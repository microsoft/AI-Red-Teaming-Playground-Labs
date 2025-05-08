# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from dataclasses import dataclass
from dataclasses_json import DataClassJsonMixin
from typing import Optional

EVENT_HEARTBEAT = "ping"
EVENT_SCORE_CONVERSATION = "score_conversation"
EVENT_ACTIVITY_SIGNAL = "activity_signal"

EVENT_CLIENT_STATUS_UPDATE = "client_status_update"
EVENT_CLIENT_REVIEW_UPDATE = "client_review_update"
EVENT_CLIENT_REVIEW_DONE = "client_review_done"
EVENT_CLIENT_TIME_UPDATE = "client_time_update"

EVENT_CLIENT_SERVER_ERROR = "client_server_error"

ROOM_DEFAULT_BROADCAST = "scorer"


@dataclass
class ConversationStatusResponse(DataClassJsonMixin):
  id: int
  guid: str
  challenge_id: int
  in_review: bool

@dataclass
class ConversationStatus(DataClassJsonMixin):
  id: int
  guid: str
  challenge_id: int
  assigned_to: str

  def to_response(self) -> ConversationStatusResponse:
    return ConversationStatusResponse(
      id=self.id,
      guid=self.guid,
      challenge_id=self.challenge_id,
      in_review=self.assigned_to is not None and self.assigned_to != ""
    )


@dataclass
class ChatMessage(DataClassJsonMixin):
  role: int
  message: str

@dataclass
class ConversationReviewResponse(DataClassJsonMixin):
  id:int
  guid: str
  title: str
  goal: str
  document: str
  conversation: Optional[list[ChatMessage]]
  picture: Optional[str]

@dataclass
class ConversationReviewRequest(DataClassJsonMixin):
  id: int
  challenge_id: int
  challenge_goal: str
  challenge_title: str
  conversation: Optional[list[ChatMessage]]
  picture: Optional[str]
  timestamp: str
  conversation_id: str
  document: str
  answer_uri: str

  def to_status(self) -> ConversationStatus:
    return ConversationStatus(
      id=self.id,
      guid=self.conversation_id,
      challenge_id=self.challenge_id,
      assigned_to=""
    ) 
  
  def to_response(self) -> ConversationReviewResponse:
    return ConversationReviewResponse(
      id=self.id,
      guid=self.conversation_id,
      title=self.challenge_title,
      goal=self.challenge_goal,
      document=self.document,
      conversation=self.conversation,
      picture=self.picture
    )

@dataclass
class CurrentStatusResponse(DataClassJsonMixin):
  session_count: int
  conversation_queue: list[ConversationStatusResponse]


@dataclass
class ScoreConversationRequest(DataClassJsonMixin):
  conversation_id: str
  passed: bool
  custom_message: str

@dataclass
class ServerErrorResponse(DataClassJsonMixin):
    error_msg: str