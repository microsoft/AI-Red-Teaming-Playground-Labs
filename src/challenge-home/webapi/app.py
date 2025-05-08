# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import logging
import os
import uuid
import time

from flask import Flask, redirect, request, jsonify, make_response, render_template
from itsdangerous import URLSafeSerializer


logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__,
            static_url_path='',
            static_folder="build",
            template_folder="build")

app.config['SECRET_KEY'] = os.environ.get("SECRET_KEY", "default")
if app.config['SECRET_KEY'] == "default":
  logger.warning("SECRET_KEY is the default. Make sure you change this.")

app.config['AUTH_KEY'] = os.environ.get("AUTH_KEY", "default")
if app.config['AUTH_KEY'] == "default":
  logger.warning("AUTH_KEY is the default. Make sure you change this.")
logger.info("You can connect to the app with the url http://localhost:5000/login?auth=%s", app.config['AUTH_KEY'])

app.config['EXPIRATION'] = int(os.environ.get("EXPIRATION", 3600))
app.config['OPEN_LINK_NEW_TAB'] = os.environ.get("OPEN_LINK_NEW_TAB", "false").lower() == "true"

serializer = URLSafeSerializer(app.config['SECRET_KEY'])

def validate_cookie() -> bool:
  token = request.cookies.get('session')
  if not token:
    return False
  
  try:
    payload = serializer.loads(token)
    if payload['exp'] < int(time.time()):
      return False
    return True
  
  except Exception as e:
    return False
  
@app.before_request
def before_request():
  if request.path == '/login':
    return
  if validate_cookie():
    return
  return "Unauthorized. You must authenticate with the magic link.", 401

@app.route('/login', methods=['GET'])
def login():
  auth = request.args.get('auth')
  if auth != app.config['AUTH_KEY']:
    return jsonify({"message": "Unauthorized"}), 401


  if request.cookies.get('session') and validate_cookie():
    # Redirect to the home page since the user is already logged in
    return redirect('/', code=302)

  user_id = str(uuid.uuid4())
  exp = int(time.time()) + app.config['EXPIRATION']
  payload = {'user_id': user_id, 'exp': exp}
  token = serializer.dumps(payload)
  response = make_response(jsonify({"message": "Logged in"}))
  response.set_cookie('session', token, httponly=True, max_age=app.config['EXPIRATION'])
  if app.config['OPEN_LINK_NEW_TAB']:
    response.set_cookie('home-new-tab', 'true', httponly=False, max_age=app.config['EXPIRATION'])
  # Redirect to the home page
  response.headers['Location'] = '/'
  return response, 302

@app.route('/protected', methods=['GET'])
def protected():
  return jsonify({"message": "Protected resource"})

@app.route('/')
def index():
  return render_template("index.html")


if __name__ == '__main__':
  app.run(debug=True)