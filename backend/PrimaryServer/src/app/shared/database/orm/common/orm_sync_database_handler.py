from abc import abstractmethod
from typing import Any
from ...common import DatabaseHandler

class OrmSyncDatabaseHandler(DatabaseHandler):

    def __init__(self, uri: str) -> None:
        super().__init__(uri)

    @abstractmethod
    def getSession(self) -> Any: ...

    @abstractmethod
    def connect(self) -> None: ...

    @abstractmethod
    def disconnect(self) -> None: ...

    @abstractmethod
    def execute(self, query: str, *args: Any) -> Any: ...
