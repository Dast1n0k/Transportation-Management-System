from abc import abstractmethod
from typing import Any
from .database_handler import DatabaseHandler

class OrmAsyncDatabaseHandler(DatabaseHandler):

    def __init__(self, uri: str) -> None:
        super().__init__(uri)

    @abstractmethod
    async def getSession(self) -> Any: ...

    @abstractmethod
    def connect(self) -> None: ...

    @abstractmethod
    def disconnect(self) -> None: ...

    @abstractmethod
    def execute(self, query: str, *args: Any) -> Any: ...
