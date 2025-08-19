from abc import ABC

class DatabaseHandler(ABC):

    def __init__(self, uri: str) -> None:
        self._uri = uri
