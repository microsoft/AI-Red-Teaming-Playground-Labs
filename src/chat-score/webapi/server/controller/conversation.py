# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import os
import requests

from flask_socketio import SocketIO
from server.environ import ENV_NAME_SCORING_KEY
from server.models.conversation import ConversationModel
from server.models.connection import ConnectionModel
from server.dtos import EVENT_CLIENT_REVIEW_DONE, ConversationReviewRequest, CurrentStatusResponse, EVENT_CLIENT_STATUS_UPDATE, EVENT_CLIENT_REVIEW_UPDATE, ROOM_DEFAULT_BROADCAST, ScoreConversationRequest

class ConversationController:

  def __init__(self,
               conversation_model: ConversationModel,
               connection_model: ConnectionModel,
               socket: SocketIO):
    self.__conversation_model = conversation_model
    self.__connection_model = connection_model
    self.__socket = socket
    self.__logger = logging.getLogger(__name__)
  
  def pick(self):
    """
    Method called when a connection is ready to pick a conversation
    """

    # Try to assign the conversation to a connection
    socket_id = self.__connection_model.pop_from_pool()
    if socket_id is not None:
      conversation_id = self.__conversation_model.assign_free(socket_id)
      if conversation_id is not None:
        # So we have assigned the conversation to the connection
        self.__logger.info("Assigned conversation %s to connection %s", conversation_id, socket_id)

        # We send an update to the connection with the details of the conversation
        conversation = self.__conversation_model.get_conversation(conversation_id)
        self.__socket.emit(EVENT_CLIENT_REVIEW_UPDATE, conversation.to_response().to_json(), to=socket_id)
      else:
        # We could not assign the conversation to the connection we put it back in the pool at the front
        self.__connection_model.add_to_pool_front(socket_id)
    else:
      self.__logger.info("No connections available to assign the conversation to")

    #We send an update to all clients
    self.__send_update()
  
  def new_conversation(self, conversation: ConversationReviewRequest) -> bool:
    """
    Handle a new conversation and will try to assign it to a connection
    """
    if self.__conversation_model.get_conversation(conversation.conversation_id) is not None:
      self.__logger.error("Conversation %s already exists", conversation.conversation_id)
      return False

    conversation_status = conversation.to_status()
    id = self.__conversation_model.push(conversation_status)
    conversation.id = id
    self.__conversation_model.add(conversation)
    self.pick()
    return True

  def score(self, score_request: ScoreConversationRequest, socket_id: str):
    """
    Handle the scoring of a conversation
    """
    conversation = self.__conversation_model.get_conversation(score_request.conversation_id)
    if conversation is None:
      self.__logger.error("Conversation %s not found", score_request.conversation_id)
      return
    
    assignement = self.__conversation_model.get_assignement(socket_id)
    if assignement != score_request.conversation_id:
      self.__logger.error("Conversation %s is not assigned to %s", score_request.conversation_id, socket_id)
      return
    
    self.__conversation_model.remove(score_request.conversation_id, socket_id)
    self.__socket.emit(EVENT_CLIENT_REVIEW_DONE, {"status":"done"}, to=socket_id)
    self.__connection_model.add_to_pool(socket_id)
    self.pick()

    self.__logger.info("Scored conversation %s with %s", score_request.conversation_id, score_request.passed)
    
    url = conversation.answer_uri
    response = requests.post(url, json={
      "passed": score_request.passed,
      "custom_message": score_request.custom_message
    }, headers={
      "x-scoring-key": os.environ.get(ENV_NAME_SCORING_KEY)
    })
    response.raise_for_status()
    self.__logger.info("Request to %s completed", url)


  def dead_connections(self, dead_users: list[str]):
    """
    Handle dead connections
    """
    self.__conversation_model.unassign_review(dead_users)
    self.__send_update()

    # We try to reassign the conversation with pick
    self.pick()

  def dead_reviews(self):
    """
    Handle dead reviews
    """
    socket_ids = self.__conversation_model.unassign_expired()
    if len(socket_ids) > 0:
      for socket_id in socket_ids:
        self.__socket.emit(EVENT_CLIENT_REVIEW_DONE, {"status":"expired"}, to=socket_id)
        if self.__connection_model.is_alive(socket_id):
          self.__connection_model.add_to_pool(socket_id)
        self.pick()
      self.__send_update()


  def __send_update(self):
    session_count = self.__connection_model.get_count()
    queue = self.__conversation_model.get_queue()
    queue = [q.to_response() for q in queue]
    current_status = CurrentStatusResponse(session_count=session_count, conversation_queue=queue)
    self.__logger.info("Sending status update to all clients, %s", current_status.to_json())
    self.__socket.emit(EVENT_CLIENT_STATUS_UPDATE, current_status.to_json(), to=ROOM_DEFAULT_BROADCAST)