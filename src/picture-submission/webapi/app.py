# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import json
import redis

from flask import Flask, Response, jsonify, render_template, request
from flask_cors import CORS
from azure.identity import AzureCliCredential, DefaultAzureCredential
from azure.storage.blob import BlobServiceClient

from server.middleware.scoring_auth import scoring_auth
from server.middleware.ctfd_auth import ctfd_auth
from server.service.ctfd.ctfd import CtfdService, set_ctfd_service_instance
from server.service.ctfd.ticket import CtfdAuthTicket

from server.controller.submission import SubmissionController
from server.controller.scoring import ScoringController
from server.models.submission import SubmissionModel
from server.dtos import ChallengeSettingsResponse
from server.settings import CONFIG_AUTH_SETTINGS, CONFIG_AUTH_TYPE, CONFIG_CHALLENGE_SETTINGS, CONFIG_CHALLENGE_ID, CONFIG_CHALLENGE_NAME, CONFIG_CHALLENGE_DESCRIPTION, CONFIG_REDIS_URL, CONFIG_STORAGE_ACCOUNT_URL

app = Flask(__name__,
            static_url_path='',
            static_folder='build',
            template_folder='build')

app.config.from_file('config.json', load=json.load)
app.config.from_prefixed_env()
CORS(app, supports_credentials=True)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

account_url = app.config[CONFIG_STORAGE_ACCOUNT_URL]
if app.debug:
    default_credential = AzureCliCredential()
else:
    default_credential = DefaultAzureCredential()
blob_service_client = BlobServiceClient(account_url=account_url, credential=default_credential)

r = redis.Redis.from_url(app.config[CONFIG_REDIS_URL])
submission_model = SubmissionModel(r, blob_service_client, app)

# Setup the CTFd service
if app.config[CONFIG_AUTH_SETTINGS][CONFIG_AUTH_TYPE] == "ctfd":
    logger.info("Setting up CTFd Service")
    set_ctfd_service_instance(CtfdService(app, submission_model))

scoring_controller = ScoringController(app, submission_model)
submission_controller = SubmissionController(app, submission_model, scoring_controller)

@app.route('/')
def index():
  return render_template("index.html")

@app.route("/healthz", methods=["GET"])
def healthz():
    return "OK", 200

@app.route("/challenge/settings", methods=["GET"])
@ctfd_auth
def get_challenge_settings(**_):
    id = app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_ID]
    name = app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_NAME]
    description = app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_DESCRIPTION]

    response = ChallengeSettingsResponse(id, name, description)
    return jsonify(response.to_dict())

@app.route("/status", methods=["GET"])
@ctfd_auth
def get_status(ctfd_ticket: CtfdAuthTicket):
    return submission_controller.get_status(ctfd_ticket.id)

@app.route("/picture", methods=["GET"])
@ctfd_auth
def get_picture(ctfd_ticket: CtfdAuthTicket):
    return submission_controller.get_picture(ctfd_ticket.id)

@app.route("/upload", methods=["POST"])
@ctfd_auth
def upload(ctfd_ticket: CtfdAuthTicket):
    return submission_controller.upload_picture(ctfd_ticket)

@app.route("/score/<id>", methods=["POST"])
@scoring_auth
def score(id: str):
    data = request.get_json()
    for key in ["passed", "custom_message"]:
        if key not in data:
            return Response(f"Missing key: {key}", status=400)
    
    passed = data["passed"]
    message = data["custom_message"]

    return scoring_controller.set_score(id, passed, message)

if __name__ == "__main__":
    app.run(debug=True)