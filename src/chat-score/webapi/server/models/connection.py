# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from typing import Tuple
from redis import Redis

from server.keys import REDIS_CONNECTION_COUNT, REDIS_CONNECTION_COUNT_KEY, REDIS_CONNECTION_POOL, REDIS_CONNECTION_SET

CONNECTION_COUNT_TTL = 7 # Seconds

class ConnectionModel:
  def __init__(self, redis: Redis):
    self.__r = redis

  def increment(self, id: str) -> int:
    """
    Function to increment the connection count and set the key for the given id
    """
    result = None
    with self.__r.pipeline() as pipe:
      pipe.multi()
      pipe.incr(REDIS_CONNECTION_COUNT)
      pipe.set(f"{REDIS_CONNECTION_COUNT_KEY}{id}", 1, ex=CONNECTION_COUNT_TTL)
      pipe.hset(REDIS_CONNECTION_SET, id, 1)
      pipe.lpush(REDIS_CONNECTION_POOL, id)
      result = pipe.execute()
    
    if result == None:
      return 0 
    
    return result[0]
  
  def extend(self, id: str):
    """
    Function to extend the TTL of the connection count key for the given id
    """
    with self.__r.pipeline() as pipe:
      pipe.multi()
      pipe.set(f"{REDIS_CONNECTION_COUNT_KEY}{id}", 1, ex=CONNECTION_COUNT_TTL)
      pipe.hset(REDIS_CONNECTION_SET, id, 1)
      pipe.execute()

  def is_alive(self, id: str) -> bool:
    """
    Function to check if the connection is alive
    """
    result = self.__r.get(f"{REDIS_CONNECTION_COUNT_KEY}{id}")
    return result is not None

  def get_count(self) -> int:
    """
    Function to get the connection count
    """
    result = self.__r.get(REDIS_CONNECTION_COUNT)
    if result is None:
      return 0
    return int(result)
  
  def pop_from_pool(self) -> str:
    """
    Function to pop a connection from the pool
    """
    result = self.__r.rpop(REDIS_CONNECTION_POOL)
    if result is None:
      return result
    return result.decode("utf-8")
  
  def add_to_pool(self, id: str):
    """
    Function to add a connection to the pool
    """
    self.__r.lpush(REDIS_CONNECTION_POOL, id)

  def add_to_pool_front(self, id: str):
    """
    Function to add a connection to the pool
    """
    self.__r.rpush(REDIS_CONNECTION_POOL, id)

  def integrity(self) -> Tuple[bool, int, list[str]]:
    """
    Function to check the integrity of the connection count. It will remove any keys that are not present in the count
    and update the count accordingly.
    """

    # We don't really need to worry about a lock since all the write operations are atomic
    # and this task is only run by one worker at a time
    
    data = self.__r.hgetall(REDIS_CONNECTION_SET)

    # Check if the key exists
    if not data:
      self.__r.set(REDIS_CONNECTION_COUNT, 0)
      return (False, 0, [])
    
    count = len(data)
    changed = False
    users = []
    for key in data.keys():
      key = key.decode("utf-8")
      exists = self.__r.get(f"{REDIS_CONNECTION_COUNT_KEY}{key}")
      if exists is None:
        self.__r.hdel(REDIS_CONNECTION_SET, key)

        # Remove from the pool
        self.__r.lrem(REDIS_CONNECTION_POOL, 0, key)
        users.append(key)
        count -= 1
        changed = True

    self.__r.set(REDIS_CONNECTION_COUNT, count)
    return (changed, count, users)