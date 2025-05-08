# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
from redis import Redis

REDIS_MAX_LOCK = 10 #seconds

class RedisLock:

    def __init__(self, redis: Redis, lock_name: str, timeout=REDIS_MAX_LOCK):
        self.__r = redis
        self.__name = lock_name
        self.__timeout = timeout
        self.__registered = False
        self.__logger = logging.getLogger(__name__)
        self.__count = 1

    def start(self, count=1):
        """
        Start the lock with the number of instances. If empty the lock primitive will
        be created
        """

        self.__count = count
        self.__logger.info("Starting RedisLock: %s Count: %d", self.__name, count)

        lua_script_start = """
local keyIncr = KEYS[1]
local keyInsert = KEYS[2]
local count = tonumber(ARGV[1])
local returnValue = redis.call('INCRBY', keyIncr, count)
if returnValue == count then
    returnValue = redis.call('LLEN', keyInsert)
    if returnValue > 1 then
        return 1
    end
    if returnValue == 0 then
        returnValue = redis.call('LPUSH', keyInsert, 1)
    end
end
if returnValue <= 0 then
    return 1
end
return 0
"""
        if not self.__registered:
            self.__start_lock = self.__r.register_script(lua_script_start)
            self.__registered = True
    
        result = self.__start_lock(keys=[self.__key_count(), self.__name], args=[count])
        if result == 1:
            raise ValueError("Error: The lock data is not coherent...")

    def stop(self):
        """
        Stop the lock. Remove one instance of the lock
        """
        self.__r.decr(self.__key_count(), self.__count)

    def lock_r(self):
        result = self.__r.blpop(self.__name, timeout=self.__timeout)
        if result == None:
            raise TimeoutError("Redis lock: timeout reached!")

    def release_r(self):
        self.__r.lpush(self.__name, 1)

    def __key_count(self):
        return self.__name + ".count"