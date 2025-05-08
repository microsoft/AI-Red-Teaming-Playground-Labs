# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from flask import Flask, request
import subprocess
import logging
from logging.config import dictConfig
import re

logging.basicConfig(level=logging.INFO)

dictConfig(
    {
        "version": 1,
        "formatters": {
            "default": {
                "format": "[%(asctime)s] %(levelname)s in %(module)s: %(message)s",
            }
        },
        "handlers": {
            "console": {
                "class": "logging.StreamHandler",
                "stream": "ext://sys.stdout",
                "formatter": "default",
            }
        },
        "root": {"level": "INFO", "handlers": ["console"]},
    }
)

app = Flask(__name__)

@app.route('/')
def index():
  return app.send_static_file('index.html')

@app.route('/<path:path>')
def static_file(path):
  return app.send_static_file(path)

@app.route('/execute', methods=['POST'])
def execute_code():
  code = request.get_data(as_text=True)
  app.logger.info('Received code\n%s', code)

  if re.search(r'(import\s+(os|subprocess))|(from\s+(os|subprocess))', code, re.IGNORECASE):
    response = 'Your code contains a disallowed import statement. Make sure to only use the allowed built-in functions and modules.'
    app.logger.info(f"Output: {response}")
    return response
  
  app.logger.info('Executing code...')
  try:
    output = subprocess.check_output(['python', '-'], input=code, stderr=subprocess.STDOUT, universal_newlines=True)
    if output == '' or output == '\n':
      response = 'The code executed but it did not return any output. Make sure to use print statements to output something from your script.'
    response = output
    app.logger.info(f"Output: {response}")
    return response
  except BlockingIOError as exc:
    response = f"The code failed to execute. Error: {exc}"
    app.logger.info(f"Output: {response}")
    return response
  except subprocess.CalledProcessError as exc:
    response = f"The code failed to execute. Error code: {exc.returncode}\n\n{exc.output}"
    app.logger.info(f"Output: {response}")
    return response

if __name__ == '__main__':
  app.run(host="0.0.0.0", port=5000)
