from abc import ABC
from typing import Any

class DatabaseHandler(ABC):

    def __init__(self, autoFlush: bool) -> None:
        self._autoFlush = autoFlush

    def flush(self) -> None:
        pass

    def disconnect(self) -> None:
        pass

    def connect(self, uri: str) -> None:
        pass

    def commit(self, query: str, *args: Any) -> None:
        pass
