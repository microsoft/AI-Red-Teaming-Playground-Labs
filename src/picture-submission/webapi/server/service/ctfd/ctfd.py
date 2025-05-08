# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import re
import json
import uuid
import logging
import requests

from flask import Request, Response
from redis import Redis
from itsdangerous import Signer, want_bytes
from urllib.parse import urljoin
from typing import Optional

from server.settings import CONFIG_AUTH_SETTINGS, CONFIG_CHALLENGE_ID, CONFIG_CHALLENGE_SETTINGS, CONFIG_CTFD_REDIRECT_URL, CONFIG_CTFD_REDIS_URL, CONFIG_CTFD_SECRET_KEY, CONFIG_CTFD_SETTINGS, CONFIG_CTFD_URL
from server.dtos import AuthErrorResponse
from server.service.cache import CacheTTL
from server.models.submission import SubmissionModel
from server.service.ctfd.ticket import CtfdAuthTicket

global __CTFD_SERVICE__ # type: Optional[CtfdService]

CTFD_PREFIX_NAME = "flask_cache_session"
CTFD_COOKIE_SALT = "itsdangerous.Signer"

class CtfdService:
    def __init__(self, app, submission_model: SubmissionModel):
        self.__app = app
        self.__submission_model = submission_model
        self.__redis = None
        self.__logger = logging.getLogger(__name__)
        self.__json_regex = re.compile(b"{.+}")
        self.__ctfd_settings = self.__app.config[CONFIG_AUTH_SETTINGS][CONFIG_CTFD_SETTINGS]
        self.__signer = Signer(self.__ctfd_settings[CONFIG_CTFD_SECRET_KEY], salt=CTFD_COOKIE_SALT)
        if self.__ctfd_settings[CONFIG_CTFD_REDIS_URL]:
            self.__redis = Redis.from_url(self.__ctfd_settings[CONFIG_CTFD_REDIS_URL])
        
        self.__cache_ttl = CacheTTL(60) # 60 seconds

        self.__ctfd_url = self.__ctfd_settings[CONFIG_CTFD_URL]
        self.__challenge_id = self.__app.config[CONFIG_CHALLENGE_SETTINGS][CONFIG_CHALLENGE_ID]

    def validate_auth(self, request: Request) -> tuple[bool, Optional[CtfdAuthTicket], Optional[Response]]:
        def fail_response(reason: str) -> Response:
            self.__logger.error(f"CTFd Auth failed: {reason}")
            response = AuthErrorResponse("ctfd", reason, self.__ctfd_settings[CONFIG_CTFD_REDIRECT_URL])
            return Response(response.to_json(), status=401, mimetype="application/json")

        if self.__redis is None:
            return False, None, fail_response("Redis not configured")
        
        session = request.cookies.get("session")
        if session is None:
            return False, None, fail_response("No session cookie")
        
        # Check if the session cookie is in the TTL Cache
        cached_session = self.__cache_ttl.get(session)
        if cached_session is not None:
            self.__logger.info("Session cookie found in cache")
            return True, cached_session, None
        
        session_cookie_split = session.split(".")
        if len(session_cookie_split) < 2:
            return False, None, fail_response("Invalid session cookie format")
        
        session_id = session_cookie_split[0]
        if not self.__is_valid_uuid(session_id):
            return False, None, fail_response("Invalid session ID")
        
        signature = session_cookie_split[1]
        valid_signature = self.__sign(session_id)
        if signature != valid_signature:
            return False, None, fail_response("Invalid session cookie signature")
        
        # Get the session data from Redis
        key_name = f"{CTFD_PREFIX_NAME}{session_id}"
        session_data = self.__redis.get(key_name)
        if session_data is None:
            return False, None, fail_response("Session not found in Redis")
        
        matches = re.findall(self.__json_regex, session_data)
        if len(matches) == 0:
            return False, None, fail_response("Invalid session data format")
        
        json_data = matches[0]
        session_dict = json.loads(json_data)
        if "id" not in session_dict or "nonce" not in session_dict:
            return False, None, fail_response("Invalid session value. Missing required values.")

        id = int(session_dict["id"])
        nonce = session_dict["nonce"]

        ticket = CtfdAuthTicket(id, nonce, session)
        self.__logger.info(f"Session cookie validated: {session_id}")

        self.__cache_ttl.set(session, ticket)

        return True, ticket, None
    
    def send_flag(self, user_id: str, flag: str) -> True:
        self.__logger.info(f"Marking submission {user_id} as correct in CTFd")
        submission = self.__submission_model.get_submission(user_id)
        if submission is None:
            self.__logger.error(f"Submission {user_id} not found in the database")
            return False
        
        if submission.ticket is None:
            self.__logger.error(f"Submission {user_id} does not have a ticket")
            return False
        
        ticket = submission.ticket

        headers = {
            "Cookie":f"session={ticket.cookie}",
            "CSRF-Token":ticket.nonce, 
        }
        
        url = urljoin(self.__ctfd_url, f"/api/v1/challenges/{self.__challenge_id}")
        
        try:
            request = requests.get(url, headers=headers)
            request.raise_for_status()
            response = request.json()
            if "success" not in response:
                self.__logger.error(f"Invalid response from CTFd url:{url}| {response}")
                return False

            if not response["success"]:
                self.__logger.error(f"Failed to get challenge from CTFd: {response}")
                return False

            if response["data"]["solved_by_me"]:
                self.__logger.info(f"Flag already submitted for {user_id}")
                return True

            url = urljoin(self.__ctfd_url, f"/api/v1/challenges/attempt")
            request = requests.post(url, headers=headers, json={"challenge_id":self.__challenge_id, "submission":flag})
            request.raise_for_status()
            response = request.json()

            if "success" not in response:
                self.__logger.error(f"Invalid response from CTFd url:{url} | {response}")
                return False

            if not response["success"]:
                self.__logger.error(f"Failed to submit flag to CTFd: {response}")
                return False
            
            if "data" not in response or "status" not in response["data"] or response["data"]["status"] != "correct":
                self.__logger.error(f"Flag submission failed: {response}")
                return False

            return True
        except requests.exceptions.RequestException as e:
            self.__logger.error(f"Failed to send flag to CTFd: {e}", exc_info=True)
            return False
    
    def __sign(self, data: str) -> str:
        output = self.__signer.sign(want_bytes(data)).decode("utf-8")
        signature = output.split(".")[1]
        return signature

    def __is_valid_uuid(self, guid: str) -> bool:
        try:
            uuid.UUID(guid)
            return True
        except ValueError:
            return False

def get_ctfd_service_instance() -> CtfdService:
    global __CTFD_SERVICE__
    return __CTFD_SERVICE__

def set_ctfd_service_instance(instance: CtfdService) -> None:
    global __CTFD_SERVICE__
    __CTFD_SERVICE__ = instance