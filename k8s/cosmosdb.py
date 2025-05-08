# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""
Deploy/Drop the cosmosdb databases that are needed in Azure. By default deploys in the dev environment.
Usage: python usage: cosmosdb.py [-h] [--env {dev,prod,blackhat,local}] [--drop] [file]
"""

import argparse
import json
import asyncio
from azure.identity.aio import DefaultAzureCredential
from azure.cosmos.aio import CosmosClient
from azure.cosmos import PartitionKey

SUBSCRIPTION_ID = "ebf8d95b-d465-479c-b9ce-9d7889ee027e"

parser = argparse.ArgumentParser(
    description="Deploy the cosmosdb databases that are needed in Azure"
)
parser.add_argument(
    "file", nargs="?", help="The path of the json file", default="challenges.json"
)
parser.add_argument(
    "--env",
    help="The environment to deploy the databases in",
    choices=["dev", "prod", "local"],
    default="local",
)
parser.add_argument("--drop", help="Drop the databases", action="store_true")

args = parser.parse_args()

if args.env == "prod":
    COSMOSDB_URI = "COSMOSDB_URI_PROD"
    print("Using prod environment for cosmosdb")

elif args.env == "local":
    COSMOSDB_URI = "COSMOSDB_URI_LOCAL"
    print("Using local environment for cosmosdb")

elif args.env == "dev":
    COSMOSDB_URI = "COSMOSDB_URI_DEV"
    print("Using dev environment for cosmosdb")

CONTAINERS = {
    "chatsessions": "/id",
    "chatmessages": "/chatId",
    "chatmemorysources": "/chatId",
    "chatparticipants": "/userId",
}


async def create_database_if_not_exists(client: CosmosClient, database_name):
    print(f"Creating database: {database_name}")
    database = await client.create_database_if_not_exists(database_name)
    pending_tasks = []
    for container in CONTAINERS:
        pending_tasks.append(
            database.create_container_if_not_exists(
                id=container, partition_key=PartitionKey(CONTAINERS[container])
            )
        )

    await asyncio.gather(*pending_tasks)
    print(f"Database {database_name} created")


async def delete_database_if_exists(client: CosmosClient, database_name):
    print(f"Deleting database: {database_name}")
    database = client.get_database_client(database_name)
    try:
        if await database.read():
            await client.delete_database(database_name)
            print(f"Database {database_name} deleted")
    except:
        print(f"Database {database_name} does not exist")


async def main():
    # Load the json files
    if args.env != "local":
        with open(args.file, "r") as f:
            data = json.load(f)
    else:
        # Placeholder for local testing
        data = {
            "Challenges": [
                {
                    "challenge_id": 1,
                }
            ]
        }
    print("Logging in to Azure with DefaultAzureCredential")

    credential = DefaultAzureCredential()

    if "Challenges" in data:
        async with CosmosClient(COSMOSDB_URI, credential=credential) as client:
            pending_tasks = []
            for challenge in data["Challenges"]:
                if "picture" in challenge and challenge["picture"]:
                    print(f"Skipping challenge {challenge['challenge_id']} with picture")
                    continue
                
                database_name = "chat-copilot-" + str(challenge["challenge_id"])
                if args.drop:
                    pending_tasks.append(
                        delete_database_if_exists(client, database_name)
                    )
                else:
                    pending_tasks.append(
                        create_database_if_not_exists(client, database_name)
                    )

            await asyncio.gather(*pending_tasks)

    else:
        print("Error: Invalid Format")


if __name__ == "__main__":
    asyncio.run(main())
