# from abc import abstractmethod
# from typing import Any
# from ..config import DatabaseConfig
# from .database_handler import DatabaseHandler

# class RawAsyncDatabaseHandler(DatabaseHandler):

#     def __init__(self, uri: str, config: DatabaseConfig) -> None:
#         super().__init__(uri)
#         self.__config = config

#     @abstractmethod
#     async def flush(self) -> None: ...

#     @abstractmethod
#     def connect(self) -> None: ...

#     @abstractmethod
#     def disconnect(self) -> None: ...

#     @abstractmethod
#     async def execute(self, query: str, *args: Any) -> Any: ...
