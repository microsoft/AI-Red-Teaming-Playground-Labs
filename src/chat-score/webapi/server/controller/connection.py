# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
from flask_socketio import SocketIO

from server.models.connection import ConnectionModel
from server.models.conversation import ConversationModel
from server.dtos import EVENT_CLIENT_TIME_UPDATE, CurrentStatusResponse, EVENT_CLIENT_STATUS_UPDATE, ROOM_DEFAULT_BROADCAST

class ConnectionController:
  def __init__(self, 
               connection_model: ConnectionModel,
               conversation_model: ConversationModel,
               socket: SocketIO):
    self.__connection_model = connection_model
    self.__conversation_model = conversation_model
    self.__socket = socket
    self.__logger = logging.getLogger(__name__)

  def connect(self, socket_id: str):
    result = self.__connection_model.increment(socket_id)
    self.__socket.server.enter_room(socket_id, ROOM_DEFAULT_BROADCAST)

    self.__send_update(result)

  def ping(self, socket_id: str):
    self.__connection_model.extend(socket_id)
    
    # We get the time to send a time update
    time = self.__conversation_model.get_time(socket_id)
    self.__socket.emit(EVENT_CLIENT_TIME_UPDATE, str(time), to=socket_id)

  def activity_signal(self, socket_id: str):
    time = self.__conversation_model.earn_bonus(socket_id)
    self.__socket.emit(EVENT_CLIENT_TIME_UPDATE, str(time), to=socket_id)

  def dead_connections(self) -> list[str]:
    result = self.__connection_model.integrity()
    if result[0]:
      self.__logger.info("Dead connections removed new count: %d", result[1])

      # Broadcast the new count to all clients in the conversation controller
      for user in result[2]:
        self.__socket.server.leave_room(user, ROOM_DEFAULT_BROADCAST)
        self.__socket.server.disconnect(user)
      return result[2]
    else:
      self.__logger.info("No dead connections found")
      return []

  def __send_update(self, session_count: int):
    queue = self.__conversation_model.get_queue()
    queue = [q.to_response() for q in queue]
    current_status = CurrentStatusResponse(session_count=session_count, conversation_queue=queue)
    self.__logger.info("Sending status update to all clients, %s", current_status.to_json())
    self.__socket.emit(EVENT_CLIENT_STATUS_UPDATE, current_status.to_json(), to=ROOM_DEFAULT_BROADCAST)
