# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import threading
import time
import logging

class CacheTTL:
    def __init__(self, ttl: int):
        self.__ttl = ttl
        self.__data = {}
        self.__clean_expired = time.time() + self.__ttl * 5 # Clean every 5 cycles of the TTL
        self.__logger = logging.getLogger(__name__)
        self.__lock = threading.Lock()
    
    def set(self, key: str, value: any):
        self.__data[key] = {"value": value, "expires": time.time() + self.__ttl}
        self.__clean()
    
    def get(self, key: str) -> any:
        self.__clean()

        if key not in self.__data:
            return None
        
        if self.__data[key]["expires"] < time.time():
            del self.__data[key]
            return None

        # Extend the TTL
        self.__data[key]["expires"] = time.time() + self.__ttl
        return self.__data[key]["value"]
    
    def __clean(self):
        current_time = time.time()
        if current_time > self.__clean_expired:
            try:
                self.__lock.acquire()
                self.__logger.info("Cleaning expired cache entries")
                count = 0
                keys = list(self.__data.keys())
                for key in keys:
                    if self.__data[key]["expires"] < current_time:
                        del self.__data[key]
                        count += 1
                self.__logger.info(f"Cleaned {count} expired cache entrie(s)")
                self.__clean_expired = current_time + self.__ttl * 5
            finally:
                self.__lock.release()