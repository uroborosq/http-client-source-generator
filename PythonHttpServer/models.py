from datetime import datetime
from pydantic import BaseModel


class Manager(BaseModel):
    full_name: str


class Request(BaseModel):
    client: str
    manager: Manager
    date_begin: datetime
