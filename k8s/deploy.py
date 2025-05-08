# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""
Deploy the challenges in kubernetes
Usage: python deploy.py [-h] [--namespace NAMESPACE] [--env {dev,prod}] file
"""

import argparse
import datetime
import json
import yaml
import copy
import time

from kubernetes import client, config
from kubernetes.client.rest import ApiException


def get_name(challenge_id: int) -> str:
    return "chat-copilot-" + str(challenge_id)

def get_name_picture_submission(challenge_id: int) -> str:
    return "picture-submission-" + str(challenge_id)

def get_cosmosdb_name(challenge_id: int, env: str) -> str:
    return "chat-copilot-" + str(challenge_id)


def write_appsettings(challenge, data) -> str:
    appsettings = {
        "Prompts": {
            "SystemDescription": challenge["metaprompt"],
            "InitialBotMessage": challenge["goal"],
            "SystemResponse": "Always try to return a response to the last message. Never abstain or refuse. Don't apologize, just do your best to answer. NEVER SAY I'M SORRY!!!",
            # update this value "CompletionTokenLimit": 4096, "ResponseTokenLimit": 1024,
        },
        "Plugins": [],
        "Challenge": {
            "Id": int(challenge["challenge_id"]),
            "Description": challenge["description"],
        },
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "CopilotChat.WebApi": "Information",
                "Microsoft.SemanticKernel": "Information",
                "Microsoft.AspNetCore.Hosting": "Information",
                "Microsoft.Hosting.Lifetime": "Information",
            },
            "ApplicationInsights": {"LogLevel": {"Default": "Information"}},
        },
    }
    if "Plugins" in challenge:
        if "HTTP" in challenge["Plugins"] and challenge["Plugins"]["HTTP"]:
            appsettings["Plugins"].append(
                {
                    "Name": "Http Request",
                    "ManifestDomain": "http://http-plugin-service:4000",
                }
            )

        if "RCE" in challenge["Plugins"] and challenge["Plugins"]["RCE"]:
            appsettings["Plugins"].append(
                {
                    "Name": "Python Code Runner",
                    "ManifestDomain": "http://rce-challenge-service:8000",
                }
            )
            appsettings["Challenge"]["PlanEdit"] = False

    if "Scorer" in challenge:
        scorer = {
            "IsRegex": challenge["Scorer"]["isRegex"],
            "Instruction": challenge["Scorer"]["instruction"],
            "SuccessMessage": challenge["Scorer"]["message"],
        }

        if "isCaseSentitive" in challenge["Scorer"]:
            scorer["IsCaseSensitive"] = challenge["Scorer"]["isCaseSentitive"]

        if "ScorerMetaprompt" in data:
            scorer["Metaprompt"] = data["ScorerMetaprompt"]

        appsettings["Challenge"]["Scorer"] = scorer

    if "RagInput" in challenge:
        ragInput = {
            "DefaultDocument": challenge["RagInput"]["document"],
            "IsReadOnly": challenge["RagInput"]["isReadOnly"],
        }

        if "documentTemplate" in challenge["RagInput"]:
            ragInput["DocumentTemplate"] = challenge["RagInput"]["documentTemplate"]

        if "titleShort" in challenge["RagInput"]:
            ragInput["TitleShort"] = challenge["RagInput"]["titleShort"]

        if "titleLong" in challenge["RagInput"]:
            ragInput["TitleLong"] = challenge["RagInput"]["titleLong"]

        if "instruction1" in challenge["RagInput"]:
            ragInput["Instruction1"] = challenge["RagInput"]["instruction1"]

        if "instruction2" in challenge["RagInput"]:
            ragInput["Instruction2"] = challenge["RagInput"]["instruction2"]

        if "lockAfter" in challenge["RagInput"]:
            ragInput["LockAfter"] = challenge["RagInput"]["lockAfter"]

        if "firstMessage" in challenge["RagInput"]:
            ragInput["FirstMessage"] = challenge["RagInput"]["firstMessage"]

        appsettings["Challenge"]["RagInput"] = ragInput

    if "HumanScorer" in challenge and challenge["HumanScorer"]:
        humanScorer = {
            "Endpoint": "http://chat-scoring-service:5000",
            "ChatCopilotEndpoint": f"http://{get_name(challenge['challenge_id'])}-service:4000",
            # The API key is set in the secret in the deployment .yaml file
        }
        appsettings["Challenge"]["HumanScorer"] = humanScorer

    if "XssVulnerable" in challenge:
        appsettings["Challenge"]["XssVulnerable"] = challenge["XssVulnerable"]

    if "XssRegexFilter" in challenge:
        appsettings["Challenge"]["XssRegexFilter"] = challenge["XssRegexFilter"]

    if "Ctfd" in challenge:
        appsettings["Challenge"]["Ctfd"] = {}
        appsettings["Challenge"]["Ctfd"]["Flag"] = challenge["Ctfd"]["Flag"]
        if "Id" in challenge["Ctfd"]:
            appsettings["Challenge"]["Ctfd"]["ChallengeId"] = str(
                challenge["Ctfd"]["Id"]
            )

    if "PromptOptions" in challenge:
        appsettings["Prompts"]["ResponseTemperature"] = challenge["PromptOptions"][
            "Temperature"
        ]
        appsettings["Prompts"]["ResponseTopP"] = challenge["PromptOptions"]["TopP"]
        appsettings["Prompts"]["ResponsePresencePenalty"] = challenge["PromptOptions"][
            "PresencePenalty"
        ]
        appsettings["Prompts"]["ResponseFrequencyPenalty"] = challenge["PromptOptions"][
            "FrequencyPenalty"
        ]

    return json.dumps(appsettings, indent=2)


def write_config_picture_submission(challenge) -> str:
    config = {
        "CHALLENGE_SETTINGS": {
            "ID": challenge["challenge_id"],
            "NAME": challenge["challenge_title"],
            "DESCRIPTION": challenge["description"],
            "GOAL": challenge["goal"],
        },
        "SCORING_SETTINGS": {
            "SUBMISSION_ENDPOINT": f"http://{get_name_picture_submission(challenge['challenge_id'])}-service:5000"
        },
    }

    if "Ctfd" in challenge:
        config["CHALLENGE_SETTINGS"]["FLAG"] = challenge["Ctfd"]["Flag"]
    else:
        config["CHALLENGE_SETTINGS"]["FLAG"] = "NO_FLAG_SET!"
    
    return json.dumps(config, indent=2)

def check_config_map_exists(client, config_map_name, namespace):
    try:
        client.read_namespaced_config_map(name=config_map_name, namespace=namespace)
        return True
    except ApiException as e:
        if e.status == 404:
            return False
        else:
            raise


def check_deployment_exists(client, name, namespace):
    try:
        client.read_namespaced_deployment(name=name, namespace=namespace)
        return True
    except ApiException as e:
        if e.status == 404:
            return False
        else:
            raise


def check_service_exists(client, name, namespace):
    try:
        client.read_namespaced_service(name=name, namespace=namespace)
        return True
    except ApiException as e:
        if e.status == 404:
            return False
        else:
            raise


def check_if_ingress_exists(client, name, namespace):
    try:
        client.read_namespaced_ingress(name=name, namespace=namespace)
        return True
    except ApiException as e:
        if e.status == 404:
            return False
        else:
            raise


def check_if_job_exists(client, name, namespace):
    try:
        client.read_namespaced_job(name=name, namespace=namespace)
        return True
    except ApiException as e:
        if e.status == 404:
            return False
        elif e.status == 409:
            return True
        else:
            raise


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Deploy in kubernetes the challenges")
    parser.add_argument(
        "file", help="The path of the json file", default="challenges.json"
    )
    parser.add_argument(
        "--namespace", help="The namespace to deploy the challenges", default="dev"
    )
    parser.add_argument(
        "--env",
        help="The environment to deploy the challenges in",
        choices=["dev", "prod"],
        default="dev",
    )

    args = parser.parse_args()

    config.load_kube_config()
    v1 = client.CoreV1Api()
    v1apps = client.AppsV1Api()
    v1networking = client.NetworkingV1Api()
    v1batch = client.BatchV1Api()

    print(f"Deploying challenges in namespace: {args.namespace}")
    print(f"Using environment: {args.env}")

    ingress_path = f"conf/{args.env}/ingress/ingress.yaml"
    ingress_chat_scoring_path = f"conf/{args.env}/ingress/ingress-chat-scoring.yaml"

    deployment_path = f"conf/{args.env}/chat-copilot-deployment.yaml"
    deployment_safety_path = f"conf/{args.env}/chat-copilot-safety-deployment.yaml"
    picture_submission_path = f"conf/{args.env}/picture-submission-deployment.yaml"
    chat_scoring_deployment_path = f"conf/{args.env}/chat-scoring-deployment.yaml"
    challenge_home_path = f"conf/{args.env}/challenge-home-deployment.yaml"

    ctfd_migration_job_path = f"conf/{args.env}/ctfd-migration-job.yaml"

    service_path = "conf/chat-copilot-service.yaml"
    picture_submission_service_path = "conf/picture-submission-service.yaml"
    chat_scoring_service_path = "conf/chat-scoring-service.yaml"

    # Load all the yaml files
    # Ingress files
    with open(ingress_path, "r") as f:
        ingress = yaml.safe_load(f)

    ingress_chat_scoring = None
    with open(ingress_chat_scoring_path, "r") as f:
        ingress_chat_scoring = yaml.safe_load(f)

    # Deployment files
    with open(deployment_path, "r") as f:
        deployment_yml = yaml.safe_load(f)

    with open(deployment_safety_path, "r") as f:
        deployment_safety_yml = yaml.safe_load(f)


    deployment_picture_submission = None
    with open(picture_submission_path, "r") as f:
        deployment_picture_submission = yaml.safe_load(f)

    deployment_chat_scoring = None
    with open(chat_scoring_deployment_path, "r") as f:
        deployment_chat_scoring = yaml.safe_load(f)

    with open(challenge_home_path, "r") as f:
        challenge_home_deployment = yaml.safe_load(f)

    # Job files
    ctfd_migration_job = None
    with open(ctfd_migration_job_path, "r") as f:
        ctfd_migration_job = yaml.safe_load(f)

    # Service files
    with open(service_path, "r") as f:
        service_yml = yaml.safe_load(f)

    with open(picture_submission_service_path, "r") as f:
        service_picture_submission_yml = yaml.safe_load(f)

    service_chat_scoring_yml = None
    with open(chat_scoring_service_path, "r") as f:
        service_chat_scoring_yml = yaml.safe_load(f)

    # Load the challenge json file
    with open(args.file, "r") as f:
        data = json.load(f)

    if "Challenges" in data:
        ingress_paths = []
        challenges_settings = []
        ctfd = False
        picture = False

        for challenge in data["Challenges"]:
            if "Ctfd" in challenge:
                ctfd = True

            if "picture" in challenge and challenge["picture"]:
                picture = True

            ingress_path = copy.deepcopy(
                ingress["spec"]["rules"][0]["http"]["paths"][0]
            )

            if not picture:
                if "safetyFilters" in challenge and challenge["safetyFilters"]:
                    deployment = copy.deepcopy(deployment_safety_yml)
                else:
                    deployment = copy.deepcopy(deployment_yml)
                service = copy.deepcopy(service_yml)

                name = get_name(challenge["challenge_id"])
                cosmosdb_name = get_cosmosdb_name(challenge["challenge_id"], args.env)
                config_map_name = name + "-conf"
                service_name = name + "-service"

                model = "gpt-4o-blackhat"
                if "model" in challenge:
                    model = challenge["model"]

                # Create the config map for the metaprompt
                config_map = client.V1ConfigMap(
                    api_version="v1",
                    kind="ConfigMap",
                    metadata=client.V1ObjectMeta(
                        name=config_map_name, namespace=args.namespace
                    ),
                    data={
                        "appsettings.Production.json": write_appsettings(challenge, data),
                        "metaprompt_leak": str(challenge["MetapromptLeak"]),
                        "challenge_name": challenge["challenge_title"],
                        "upload": str(challenge["Upload"]),
                        "plugins_control": str(challenge["PluginsControl"]),
                        "open_ai_text_deployment": model,
                        "cosmos_database": cosmosdb_name,
                    },
                )
            else:
                deployment = copy.deepcopy(deployment_picture_submission)
                service = copy.deepcopy(service_picture_submission_yml)

                name = get_name_picture_submission(challenge["challenge_id"])
                config_map_name = name + "-conf"
                service_name = name + "-service"

                # Create the config map for the picture submission
                config_map = client.V1ConfigMap(
                    api_version="v1",
                    kind="ConfigMap",
                    metadata=client.V1ObjectMeta(
                        name=config_map_name, namespace=args.namespace
                    ),
                    data={
                        "config.json": write_config_picture_submission(challenge),
                    },
                )
            if check_config_map_exists(v1, config_map_name, args.namespace):
                v1.replace_namespaced_config_map(
                    config_map_name, args.namespace, config_map
                )
                print("Config map updated: " + config_map_name)
            else:
                v1.create_namespaced_config_map(args.namespace, config_map)
                print("Config map created: " + config_map_name)

            # Create the deployment
            annotations = {
                "kubectl.kubernetes.io/restartedAt": datetime.datetime.now().isoformat()
            }
            if not picture:
                # Prometheus is only enabled on the chat copilot challenges
                annotations.update(
                    {
                        "prometheus.io/scrape": "true",
                        "prometheus.io/path": "/metrics",
                        "prometheus.io/port": "4001"
                    }
                )

            deployment["metadata"]["name"] = name
            deployment["metadata"]["namespace"] = args.namespace
            deployment["metadata"]["labels"]["app"] = name
            deployment["spec"]["selector"]["matchLabels"]["app"] = name
            deployment["spec"]["template"]["metadata"]["labels"]["app"] = name
            deployment["spec"]["template"]["metadata"]["annotations"] = annotations
            deployment["spec"]["template"]["spec"]["containers"][0]["name"] = name
            deployment["spec"]["template"]["spec"]["volumes"][0]["configMap"][
                "name"
            ] = config_map_name

            # Iterate over the containers in the first spec template

            if not picture:
                original_conf_name = "chat-copilot-conf"
            else:
                original_conf_name = "picture-submission-conf"
            for container in deployment["spec"]["template"]["spec"]["containers"]:
                # Iterate over the environment variables in the container
                env_kept = []
                for env in container["env"]:
                    # Check if the environment variable uses a value from a ConfigMap
                    if "valueFrom" in env and "configMapKeyRef" in env["valueFrom"]:
                        # Check if the ConfigMap is 'chat-copilot-conf'
                        if (env["valueFrom"]["configMapKeyRef"]["name"] == original_conf_name):
                            # Replace the ConfigMap name
                            env["valueFrom"]["configMapKeyRef"]["name"] = config_map_name
                    if env["name"] == "Challenge__HumanScorer__ApiKey" and ("HumanScorer" not in challenge or not challenge["HumanScorer"]):
                        # Remove the environment variable if the challenge doesn't have a human scorer
                        continue
                    env_kept.append(env)

                container["env"] = env_kept

            if check_deployment_exists(v1apps, name, args.namespace):
                v1apps.replace_namespaced_deployment(name, args.namespace, deployment)
                print("Deployment updated: " + name)
            else:
                v1apps.create_namespaced_deployment(args.namespace, deployment)
                print("Deployment created: " + name)

            # Create the service
            service["metadata"]["name"] = service_name
            service["spec"]["selector"]["app"] = name

            if check_service_exists(v1, service_name, args.namespace):
                v1.replace_namespaced_service(service_name, args.namespace, service)
                print("Service updated: " + service_name)
            else:
                v1.create_namespaced_service(args.namespace, service)
                print("Service created: " + service_name)

            # Add the path to the ingress
            ingress_path["path"] = f"/challenge/{challenge['challenge_id']}/(.*)"
            ingress_path["backend"]["service"]["name"] = service_name
            ingress_paths.append(ingress_path)

            if not ctfd:
                # Add Json object to the data home
                challenges_settings.append(
                    {
                        "id": str(challenge["challenge_id"]),
                        "name": challenge["challenge_title"],
                        "description": challenge["goal"],
                        "category": challenge["category"],
                    }
                )

                # Update the config map for the home page
                config_map_name = "challenge-home-conf"
                config_home = client.V1ConfigMap(
                    api_version="v1",
                    kind="ConfigMap",
                    metadata=client.V1ObjectMeta(
                        name=config_map_name, namespace=args.namespace
                    ),
                    data={"data.json": json.dumps(challenges_settings, indent=2)},
                )
                if check_config_map_exists(v1, config_map_name, args.namespace):
                    v1.replace_namespaced_config_map(
                        config_map_name, args.namespace, config_home
                    )
                    print("Config map updated: " + config_map_name)
                else:
                    v1.create_namespaced_config_map(args.namespace, config_home)
                    print("Config map created: " + config_map_name)

                # Create the deployment for the home page
                if check_deployment_exists(v1apps, "challenge-home", args.namespace):
                    annotations = {
                        "kubectl.kubernetes.io/restartedAt": datetime.datetime.now().isoformat()
                    }
                    challenge_home_deployment["spec"]["template"]["metadata"][
                        "annotations"
                    ] = annotations
                    v1apps.replace_namespaced_deployment(
                        "challenge-home", args.namespace, challenge_home_deployment
                    )
                    print("Deployment updated: challenge-home")
                else:
                    v1apps.create_namespaced_deployment(
                        args.namespace, challenge_home_deployment
                    )
                    print("Deployment created: challenge-home")

            else:
                url = ""
                if args.env == "prod":
                    url = f"https://ai-red-teaming-playground-labs.westus3.cloudapp.azure.com/challenge/{challenge['challenge_id']}/"
                description = challenge["goal"]
                if "disclaimer" in challenge and challenge["disclaimer"]:
                    description = "**Disclaimer:** This challenge contains content that might be offensive for some people. If you don't feel comfortable with this material, please don't continue and attempt the challenge.\n\n" + description
                
                challenges_settings.append(
                    {
                        "id": int(challenge["Ctfd"]["Id"]),
                        "name": challenge["challenge_title"],
                        "description": description,
                        "value": int(challenge["Ctfd"]["Points"]),
                        "category": challenge["category"],
                        "flag": challenge["Ctfd"]["Flag"],
                        "connection_info": url,
                    }
                )

        if ctfd:
            config_map_name = "ctfd-conf"
            config_ctfd = client.V1ConfigMap(
                api_version="v1",
                kind="ConfigMap",
                metadata=client.V1ObjectMeta(
                    name=config_map_name, namespace=args.namespace
                ),
                data={
                    "challenges.json": json.dumps(
                        {"challenges": challenges_settings}, indent=2
                    )
                },
            )

            # Create the config map for the ctfd
            if check_config_map_exists(v1, config_map_name, args.namespace):
                v1.replace_namespaced_config_map(
                    config_map_name, args.namespace, config_ctfd
                )
                print("Config map updated: " + config_map_name)
            else:
                v1.create_namespaced_config_map(args.namespace, config_ctfd)
                print("Config map created: " + config_map_name)

            # Clear the old pods from the job
            job_name = "ctfd-migration-job"
            pods = v1.list_namespaced_pod(
                args.namespace, label_selector=f"job-name={job_name}"
            )
            for pod in pods.items:
                v1.delete_namespaced_pod(pod.metadata.name, args.namespace)
                print(f"Pod (job) deleted: {pod.metadata.name}")

            # Start the job for the ctfd migration
            if check_if_job_exists(v1batch, job_name, args.namespace):
                v1batch.delete_namespaced_job(job_name, args.namespace)
                print(f"Job deleted: {job_name}")

            # Wait for the job to be deleted
            while check_if_job_exists(v1batch, job_name, args.namespace):
                print(f"Waiting for the job to be deleted: {job_name}")
                time.sleep(1)

            v1batch.create_namespaced_job(args.namespace, ctfd_migration_job)
            print(f"Job created: {job_name}")

        # Challenge Home Ingress and Deployment
        ingress_name = "minimal-ingress"
        ingress["spec"]["rules"][0]["http"]["paths"] = ingress_paths

        # Create the ingress
        if check_if_ingress_exists(v1networking, ingress_name, args.namespace):
            v1networking.replace_namespaced_ingress(
                ingress_name, args.namespace, ingress
            )
            print("Ingress updated: minimal-ingress")
        else:
            v1networking.create_namespaced_ingress(args.namespace, ingress)
            print("Ingress created: minimal-ingress")

        # Chat Scoring Ingress and Deployment
        if ingress_chat_scoring:
            ingress_name = ingress_chat_scoring["metadata"]["name"]

            # Create the ingress for chat scoring
            if check_if_ingress_exists(v1networking, ingress_name, args.namespace):
                v1networking.replace_namespaced_ingress(
                    ingress_name, args.namespace, ingress_chat_scoring
                )
                print(f"Ingress updated: {ingress_name}")
            else:
                v1networking.create_namespaced_ingress(args.namespace, ingress_chat_scoring)
                print(f"Ingress created: {ingress_name}")

        if deployment_chat_scoring:
            # Create the deployment for the chat scoring
            deployment_name = deployment_chat_scoring["metadata"]["name"]
            if check_deployment_exists(v1apps, deployment_name, args.namespace):
                annotations = {
                    "kubectl.kubernetes.io/restartedAt": datetime.datetime.now().isoformat()
                }
                deployment_chat_scoring["spec"]["template"]["metadata"][
                    "annotations"
                ] = annotations
                v1apps.replace_namespaced_deployment(
                    deployment_name, args.namespace, deployment_chat_scoring
                )
                print(f"Deployment updated: {deployment_name}")
            else:
                v1apps.create_namespaced_deployment(args.namespace, deployment_chat_scoring)
                print(f"Deployment created: {deployment_name}")

        # Create the service for the chat scoring
        if service_chat_scoring_yml:
            service_name = service_chat_scoring_yml["metadata"]["name"]
            if check_service_exists(v1, service_name, args.namespace):
                v1.replace_namespaced_service(
                    service_name, args.namespace, service_chat_scoring_yml
                )
                print("Service updated: " + service_name)
            else:
                v1.create_namespaced_service(args.namespace, service_chat_scoring_yml)
                print("Service created: " + service_name)
else:
    print("ERROR: Invalid format")
