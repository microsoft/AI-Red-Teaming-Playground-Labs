# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging

from redis import Redis
from flask_socketio import SocketIO

from server.controller.connection import ConnectionController
from server.controller.conversation import ConversationController
from server.models.lock import RedisLock
from server.models.connection import ConnectionModel
from server.models.conversation import ConversationModel
from worker.common.base import BaseTask

class TickTask(BaseTask):
  def __init__(self, r: Redis, lock: RedisLock, socketio: SocketIO):
    self.__lock = lock
    self.__socketio = socketio
    self.__logger = logging.getLogger(__name__)
    connection_model = ConnectionModel(r)
    conversation_model = ConversationModel(r, lock)
    self.__connection_controller = ConnectionController(connection_model, conversation_model, self.__socketio)
    self.__conversation_controller = ConversationController(conversation_model, connection_model, self.__socketio)


  def worker_ready(self, concurrency: int):
    self.__logger.info("Worker ready!")
    self.__lock.start(concurrency)

  def worker_stop(self):
    self.__logger.info("Worker stopping!")
    self.__lock.stop()
  

  def tick(self):
    self.__dead_reviews()
    self.__dead_connections()

  def __dead_reviews(self):
    # We identify if there are any dead reviews and remove them
    self.__logger.info("Running dead_reviews task")
    self.__conversation_controller.dead_reviews()
    self.__logger.info("Dead reviews task complete")

  def __dead_connections(self):
    # We identify if there are any dead connections and remove them
    self.__logger.info("Running dead_connections task")
    dead_users = self.__connection_controller.dead_connections()
    if len(dead_users) > 0:
      self.__logger.info("Dead users found %s", dead_users)
      self.__conversation_controller.dead_connections(dead_users)
    self.__logger.info("Dead connections task complete")