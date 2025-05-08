FROM ctfd/ctfd:3.7.6
RUN pip install psycopg2-binary

# Adding the update_challenges.py script to the container
COPY update_challenges.py /opt/CTFd/update_challenges.py
COPY utils/challenges/__init__.py /opt/CTFd/CTFd/utils/challenges/__init__.py
COPY utils/decorators/__init__.py /opt/CTFd/CTFd/utils/decorators/__init__.py
COPY challenges_append.js /opt/CTFd/challenges_append.js

# Append the challenges_append.js script to the challenges.js file
RUN cat /opt/CTFd/challenges_append.js >> /opt/CTFd/CTFd/themes/core-beta/static/assets/challenges.0e43adc4.js && \
  rm /opt/CTFd/challenges_append.js