# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from worker.common.conf import *
from worker.common.imports import *
from worker.tasks.tick import TickTask
from server.models.lock import RedisLock
from server.keys import REDIS_LOCK_NAME

lock = RedisLock(r, REDIS_LOCK_NAME)
tick_task = TickTask(r, lock, socket_io)

@worker_ready.connect
def init_worker(**kwargs):
    tick_task.worker_ready(kwargs["sender"].controller.concurrency)

@worker_shutting_down.connect
def stop_worker(**kwargs):
    tick_task.worker_stop()
  
@celery.task(name="common.tick5s")
def ticks5s():
    tick_task.tick()