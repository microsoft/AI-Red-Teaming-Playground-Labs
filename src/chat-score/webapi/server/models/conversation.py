# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from redis import Redis

from server.models.lock import RedisLock
from server.dtos import ConversationReviewRequest, ConversationStatus
from server.keys import REDIS_CONVERSATION_ASSIGNMENT, REDIS_CONVERSATION_COUNT, REDIS_CONVERSATION_KEY, REDIS_CONVERSATION_QUEUE, REDIS_CONVERSATION_TTL_KEY

CONVERSATION_TTL = 60 # 60 Seconds for a review
ACTIVITY_BONUS = 6 # Number of seconds you get when you send an activity signal

class ConversationModel:
  def __init__(self, redis: Redis, lock: RedisLock):
    self.__r = redis
    self.__lock = lock

  def push(self, conversation: ConversationStatus) -> int:
    """
    Function to push a conversation to the queue
    """
    self.__lock.lock_r()
    try:
      id = int(self.__r.incr(REDIS_CONVERSATION_COUNT))
      conversation.id = id
      self.__r.rpush(REDIS_CONVERSATION_QUEUE, conversation.to_json())
      return id
    finally:
      self.__lock.release_r()

  def add(self, conversation_details: ConversationReviewRequest):
    """
    Function to add a conversation details to the db
    """
    self.__lock.lock_r()
    try:
      self.__r.set(f"{REDIS_CONVERSATION_KEY}{conversation_details.conversation_id}", conversation_details.to_json())
    finally:
      self.__lock.release_r()

  def get_time(self, socket_id: str) -> int:
    ttl = self.__r.ttl(f"{REDIS_CONVERSATION_TTL_KEY}{socket_id}")
    if ttl is None or ttl < 0:
      return 0
    return ttl

  def earn_bonus(self, socket_id: str) -> int:
    """
    Function to apply a bonus to the connection TTL. Also returns the new TTL
    """
    self.__lock.lock_r()
    try:
      key = f"{REDIS_CONVERSATION_TTL_KEY}{socket_id}"
      ttl = self.__r.ttl(key)
      if ttl is None or ttl < 0:
        return 0
      new_ttl = ttl + ACTIVITY_BONUS
      if new_ttl > CONVERSATION_TTL:
        new_ttl = CONVERSATION_TTL
      self.__r.expire(key, time=new_ttl)
      return new_ttl
    finally:
      self.__lock.release_r()

  def unassign_review(self, socket_ids: list[str]):
    """
    Function to unassign a conversation from a list of reviewers
    """
    self.__lock.lock_r()
    try:
      queue = self.__get_queue()
      for i, c in enumerate(queue):
        if c.assigned_to in socket_ids:
          socket_id = c.assigned_to
          c.assigned_to = ""
          with self.__r.pipeline() as pipe:
            pipe.multi()
            pipe.lset(REDIS_CONVERSATION_QUEUE, i, c.to_json())
            pipe.hdel(REDIS_CONVERSATION_ASSIGNMENT, socket_id)
            pipe.delete(f"{REDIS_CONVERSATION_TTL_KEY}{socket_id}")
            pipe.execute()
    finally:
      self.__lock.release_r()

  def assign_free(self, assigned_to: str) -> str:
    """
    Function to assign an unassigned conversation to a reviewer
    """
    self.__lock.lock_r()
    try:
      queue = self.__get_queue()
      for i, c in enumerate(queue):
        if c.assigned_to is None or c.assigned_to == "":
          c.assigned_to = assigned_to
          with self.__r.pipeline() as pipe:
            pipe.multi()
            pipe.lset(REDIS_CONVERSATION_QUEUE, i, c.to_json())
            pipe.hset(REDIS_CONVERSATION_ASSIGNMENT, assigned_to, c.guid)
            pipe.set(f"{REDIS_CONVERSATION_TTL_KEY}{assigned_to}", c.guid, ex=CONVERSATION_TTL)
            pipe.execute()
          return c.guid
      return None
    finally:
      self.__lock.release_r()

  def get_assignement(self, assigned_to: str) -> str:
    """
    Function to get the conversation guid assigned to a reviewer
    """
    self.__lock.lock_r()
    try:
      result = self.__r.hget(REDIS_CONVERSATION_ASSIGNMENT, assigned_to)
      if result is not None:
        return result.decode("utf-8")
      return result
    finally:
      self.__lock.release_r()

  def get_queue(self) -> list[ConversationStatus]:
    """
    Function to get the conversation queue
    """
    self.__lock.lock_r()
    try:
      return self.__get_queue()
    finally:
      self.__lock.release_r()

  def get_conversation(self, guid: str) -> ConversationReviewRequest:
    """
    Function to get the conversation details
    """
    self.__lock.lock_r()
    try:
      result = self.__r.get(f"{REDIS_CONVERSATION_KEY}{guid}")
      if result is not None:
        return ConversationReviewRequest.from_json(result.decode("utf-8"))
      return None
    finally:
      self.__lock.release_r()


  def remove(self, guid: str, socket_id: str):
    """
    Function to remove a conversation from the queue
    """
    self.__lock.lock_r()
    try:
      queue = self.__get_queue()
      for c in queue:
        if c.guid == guid:
          with self.__r.pipeline() as pipe:
            pipe.multi()
            pipe.lrem(REDIS_CONVERSATION_QUEUE, 0, c.to_json())
            pipe.delete(f"{REDIS_CONVERSATION_KEY}{guid}")
            pipe.hdel(REDIS_CONVERSATION_ASSIGNMENT, socket_id)
            pipe.delete(f"{REDIS_CONVERSATION_TTL_KEY}{socket_id}")
            pipe.execute()
          break
    finally:
      self.__lock.release_r()

  def unassign_expired(self) -> list[str]:
    """
    Function to unassign expired conversations
    """
    socket_ids = []
    self.__lock.lock_r()
    try:
      queue = self.__get_queue()
      for i, c in enumerate(queue):
        if c.assigned_to is not None and c.assigned_to != "":
          ttl = self.__r.ttl(f"{REDIS_CONVERSATION_TTL_KEY}{c.assigned_to}")
          if ttl is None or ttl < 0:
            socket_ids.append(c.assigned_to)
            c.assigned_to = ""
            with self.__r.pipeline() as pipe:
              pipe.multi()
              pipe.lset(REDIS_CONVERSATION_QUEUE, i, c.to_json())
              pipe.hdel(REDIS_CONVERSATION_ASSIGNMENT, c.assigned_to)
              pipe.delete(f"{REDIS_CONVERSATION_TTL_KEY}{c.assigned_to}")
              pipe.execute()
    finally:
      self.__lock.release_r()
    return socket_ids

  def __get_queue(self) -> list[ConversationStatus]:
    """
    Function to get the conversation queue
    """
    queue = self.__r.lrange(REDIS_CONVERSATION_QUEUE, 0, -1)
    return [ConversationStatus.from_json(q) for q in queue]