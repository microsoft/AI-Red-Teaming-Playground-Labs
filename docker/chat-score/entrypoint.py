# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import os
import subprocess
import signal
import sys
import threading
import time

# List of commands to run as subprocesses
tasks = [
    {
        "task": "CWorker",
        "command": ["celery", "--app", "worker.general.celery", "worker", "--loglevel=INFO", "-c", "1", "-Q", "common"],
    },
    {
        "task": "CBeat",
        "command": ["celery", "--app", "worker.common.conf.celery", "beat", "--loglevel=INFO"]
    },
    {
        "task": "Gunicorn",
        "command": ["gunicorn", "-w", "1","--threads", "100", "-b", "0.0.0.0:5000", "--log-level", "info", "app:create_app()", "--access-logfile", "-", "--error-logfile", "-"]
    }
]

# Store subprocesses
processes = []

lock = threading.Lock()
sigint_received = False

# Function to handle SIGINT and forward it to subprocesses
def handle_sigint(signum, frame):
    global sigint_received
    sigint_received = True
    safe_print("[Orchestrator] SIGINT received, forwarding to subprocesses...")
    for process in processes:
        if process.poll() is None:
            # Only send signal if process is still running
            process.send_signal(signum)

def safe_print(*args, **kwargs):
    global lock
    with lock:
        print(*args, **kwargs)


# Function to read and print stdout and stderr without intertwining
def read_output(name, process, stream, lock):
    while True:
        output = stream.readline()
        if process.poll() is not None:
            break
        if output:
            with lock:
                print(f"[{name}-{process.pid}]\t", end="")
                print(output, end='')
                # print(output.decode("utf-8").strip())

# Register the SIGINT handler
signal.signal(signal.SIGTERM, handle_sigint)
signal.signal(signal.SIGINT, handle_sigint)


safe_print("[Orchestrator]\tStarting subprocesses...")
# Launch subprocesses
for i, task in enumerate(tasks):
    name = task['task']
    command = task['command']
    safe_print(f"[Orchestrator]\tLaunching subprocess {name}")
    safe_print("[Orchestrator]\tCommand: ", command)
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, universal_newlines=True)
    processes.append(process)
    threading.Thread(target=read_output, args=(name, process, process.stdout, lock)).start()
    threading.Thread(target=read_output, args=(name, process, process.stderr, lock)).start()

safe_print("[Orchestrator]\tSubprocesses started")

# Monitor subprocesses
try:
    while True:
        if len(processes) == 0:
            safe_print("[Orchestrator]\tAll subprocesses completed")
            sys.exit(0)

        toRemove = []
        for process in processes:
            if process.poll() is not None:
                if not sigint_received:
                    safe_print(f"[Orchestrator]\tProcess {process.pid} exited with code {process.returncode}")
                    toRemove.append(process)
                else:
                    safe_print(f"[Orchestrator]\tProcess {process.pid} exited with code {process.returncode} from SIGINT")
                    toRemove.append(process)
        
        if len(toRemove) > 0:
            processes = [p for p in processes if p not in toRemove]
            if not sigint_received:
                os.kill(os.getpid(), signal.SIGINT) # We want to exit if any process exits unexpectedly
    
        time.sleep(0.5)

except Exception as e:
    safe_print(f"Error: {e}")
    os.kill(os.getpid(), signal.SIGINT)
