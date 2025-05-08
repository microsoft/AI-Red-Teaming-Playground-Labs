# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import argparse
import json

from dataclasses import dataclass

from CTFd import create_app
from CTFd.cache import clear_challenges, clear_config, clear_standings, clear_pages
from CTFd.models import Challenges
from CTFd.models import Flags

@dataclass
class ChallengeJson:
    id: int
    name: str
    description: str
    value: int
    category: str
    flag: str
    connection_info: str

def update_challenges(path: str):
    print(f"Loading CTFd app")
    app = create_app()
    with app.app_context():
        db = app.db

        print(f'Updating challenges from {path}...')
        # Read the challenges from the JSON file
        with open(path, 'r') as f:
            challenges = json.load(f)["challenges"]
            challenges = [ChallengeJson(**challenge) for challenge in challenges]
            for challenge in challenges:
                challenge_log_name = f"{challenge.id}|{challenge.name}"
                print(f'Updating challenge {challenge_log_name}...')

                # Get the challenge from the database
                db_challenge = Challenges.query.filter_by(id=challenge.id).first()
                if db_challenge is None:
                    print(f'Challenge {challenge_log_name} not found.')
                    # Create the flag as well
                    flag = Flags(
                        challenge_id=challenge.id,
                        type='static',
                        content=challenge.flag,
                    )
                    
                    # Create the challenge if it doesn't exist
                    new_challenge = Challenges(
                        id=challenge.id,
                        name=challenge.name,
                        description=challenge.description,
                        connection_info=challenge.connection_info,
                        value=challenge.value,
                        category=challenge.category,
                    )

                    db.session.add(new_challenge)
                    db.session.commit()
                    db.session.add(flag)
                    db.session.commit()
                    print(f'Challenge {challenge_log_name} created.')
                else:
                    # Update the challenge
                    db_challenge.name = challenge.name
                    db_challenge.description = challenge.description
                    db_challenge.connection_info = challenge.connection_info
                    db_challenge.value = challenge.value
                    db_challenge.category = challenge.category
                    db.session.commit()

                    # Update the flag
                    flag = Flags.query.filter_by(challenge_id=challenge.id).first()
                    flag.content = challenge.flag
                    db.session.commit()
                    print(f'Challenge {challenge_log_name} updated.')
        
        print("Import complete!")
        db.session.close()

        print("Clearing cache...")
        clear_config()
        clear_standings()
        clear_challenges()
        clear_pages()
        print("Cache cleared.")

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Update all the challenges that are currently on the server.')
    parser.add_argument('path', type=str, help='Path to the json file containing the challenges.')
    args = parser.parse_args()

    update_challenges(args.path)