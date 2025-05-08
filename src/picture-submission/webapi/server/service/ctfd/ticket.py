# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from dataclasses import dataclass
from dataclasses_json import DataClassJsonMixin

@dataclass
class CtfdAuthTicket(DataClassJsonMixin):
    id: int
    nonce: str
    cookie: str