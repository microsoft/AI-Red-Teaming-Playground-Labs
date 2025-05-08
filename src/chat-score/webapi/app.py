# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import os
import redis
import atexit
from flask import Flask, abort, render_template, request
from flask_socketio import SocketIO

from server.controller.conversation import ConversationController
from server.controller.connection import ConnectionController
from server.models.connection import ConnectionModel
from server.models.lock import RedisLock
from server.models.conversation import ConversationModel

from server.environ import ENV_NAME_REDIS, ENV_NAME_SCORING_KEY, ENV_NAME_SECRET_KEY, REDIS_URL
from server.dtos import EVENT_ACTIVITY_SIGNAL, EVENT_CLIENT_SERVER_ERROR, EVENT_HEARTBEAT, EVENT_SCORE_CONVERSATION, ConversationReviewRequest, ScoreConversationRequest, ServerErrorResponse
from server.keys import REDIS_LOCK_NAME

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__,
            static_url_path='',
            static_folder='build',
            template_folder='build')

app.config['SECRET_KEY'] = os.environ.get(ENV_NAME_SECRET_KEY, "default")
if app.config['SECRET_KEY'] == "default":
  logger.warning("SECRET_KEY is the default. Make sure you change this in DEV")

socketio = SocketIO(app,
                    cors_allowed_origins="*",
                    logger=True,
                    message_queue=os.environ.get(ENV_NAME_REDIS, REDIS_URL))
r = redis.Redis.from_url(os.environ.get(ENV_NAME_REDIS, REDIS_URL))
lock = RedisLock(r, REDIS_LOCK_NAME)
connection_model = ConnectionModel(r)
conversation_model = ConversationModel(r, lock)
connection_controller = ConnectionController(connection_model, conversation_model, socketio)
conversation_controller = ConversationController(conversation_model, connection_model, socketio)

@socketio.on("connect")
def connect():
  socket_id = request.sid

  logger.info("Client connected %s", socket_id)
  connection_controller.connect(socket_id)
  conversation_controller.pick()

@socketio.on(EVENT_HEARTBEAT)
def ping():
  connection_controller.ping(request.sid)

@socketio.on(EVENT_ACTIVITY_SIGNAL)
def activity_signal():
   connection_controller.activity_signal(request.sid)

@socketio.on(EVENT_SCORE_CONVERSATION)
def score_conversation(data: dict):
  score_request = ScoreConversationRequest.from_dict(data)
  conversation_controller.score(score_request, request.sid)

@socketio.on_error()
def error_handler(e: Exception):
    logger.error("Uncaught error was encountered", e, exc_info=True)

    error = ServerErrorResponse(str(e))
    socketio.emit(EVENT_CLIENT_SERVER_ERROR, error.to_json())


@app.route("/api/score", methods=["POST"])
def score_chat():
  # Validate header
  auth_header = request.headers.get("x-scoring-key")
  if not auth_header:
      logger.warning("Scoring request denied due to missing authentication key.")
      abort(401)
  
  # Validate auth key
  if auth_header != os.environ.get(ENV_NAME_SCORING_KEY):
      logger.warning("Scoring request denied due to invalid authentication key.")
      abort(401)

  # Validate body
  data = request.json
  logger.debug(f"Scoring request received: {data}")
  if not data:
      logger.warning("Scoring request denied due to missing body.")
      abort(400)
  
  # Validate body fields
  for field in [
     "challenge_id",
     "challenge_goal",
     "challenge_title",
     "timestamp",
     "conversation_id",
     "answer_uri"
  ]:
     if field not in data:
          app.logger.warning(f"Scoring request denied due to missing field: {field}")
          abort(400)

  if "conversation" not in data and "picture" not in data:
      app.logger.warning("Scoring request denied due to missing field: conversation or picture")
      abort(400)
    
  if "conversation" in data and "picture" in data:
      app.logger.warning("Scoring request denied due to both fields: conversation and picture")
      abort(400)
  
  conversation = None
  picture = None
  document = None
  if "conversation" in data:
      conversation = data["conversation"]
      if "document" not in data:
          app.logger.warning("Scoring request denied due to missing field: document")
          abort(400)
      document = data["document"]
  else:
      picture = data["picture"]
      document = ""
  
  # Push to queue
  conversation_review = ConversationReviewRequest(
    id=0,
    challenge_id=data["challenge_id"],
    challenge_goal=data["challenge_goal"],
    challenge_title=data["challenge_title"],
    conversation=conversation,
    picture=picture,
    timestamp=data["timestamp"],
    conversation_id=data["conversation_id"],
    document=document,
    answer_uri=data["answer_uri"]
  )
  if conversation_controller.new_conversation(conversation_review): 
    return "OK"
  abort(409)

@app.route('/')
def index():
  return render_template("index.html")

def shutdown():
  logger.info("Shutting down...")
  lock.stop()
  logger.info("Lock stopped")

atexit.register(shutdown)

def create_app():
   lock.start()
   return app

if __name__ == "__main__":
  # Executed only in DEV mode
  lock.start()
  logger.info("Lock started")
  
  socketio.run(app)