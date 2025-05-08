# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from abc import abstractmethod
class BaseTask:
    @abstractmethod
    def worker_ready(self, concurrency: int):
        pass

    @abstractmethod
    def worker_stop(self):
        pass