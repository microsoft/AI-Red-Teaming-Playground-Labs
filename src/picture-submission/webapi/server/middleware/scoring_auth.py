# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from functools import wraps
from flask import request, Response, current_app as app

from server.settings import CONFIG_SCORING_SETTINGS, CONFIG_SCORING_KEY

def scoring_auth(f):
    @wraps(f)
    def wrapper(*args, **kwargs):
        if request.headers.get("x-scoring-key") != app.config[CONFIG_SCORING_SETTINGS][CONFIG_SCORING_KEY]:
            return Response("Unauthorized", status=401)
        return f(*args, **kwargs)
    return wrapper