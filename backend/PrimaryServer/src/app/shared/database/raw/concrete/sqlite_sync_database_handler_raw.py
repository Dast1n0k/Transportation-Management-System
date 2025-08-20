# import sqlite3
# from typing import Any, Callable, Optional
# from ..common import RawSyncDatabaseHandler

# class SqliteSyncDatabaseHandler(RawSyncDatabaseHandler):

#     def __init__(self, autoFlush: bool) -> None:
#         super().__init__(autoFlush)
#         self.__cursorHandle: Optional[sqlite3.Cursor] = None
#         self.__connectionHandle: Optional[sqlite3.Connection] = None
#         self.__commitCallback: Optional[Callable[[str, tuple[Any, ...]], None]] = None

#         if self._autoFlush:
#             self.__commitCallback = self.__commitWithFlush
#         else:
#             self.__commitCallback = self.__commitWithoutFlush

#     def __commitWithFlush(self, query: str, params: tuple[Any, ...]) -> None:
#         self.__executeQuery(query, params)
#         self.__connectionHandle.commit()

#     def __commitWithoutFlush(self, query: str, params: tuple[Any, ...]) -> None:
#         self.__executeQuery(query, params)

#     def __executeQuery(self, query: str, params: tuple[Any, ...]) -> None:
#         self.__cursorHandle.execute(query, params)

#     def flush(self) -> None:
#         if not self.__cursorHandle or not self.__connectionHandle:
#             raise RuntimeError("Database not connected")

#         self.__connectionHandle.commit()

#     def connect(self, uri: str) -> None:
#         if not uri:
#             raise ValueError("URI cannot be None or empty.")

#         self.__connectionHandle = sqlite3.connect(uri)
#         self.__cursorHandle = self.__connectionHandle.cursor()

#     def disconnect(self) -> None:
#         if self.__cursorHandle:
#             self.__cursorHandle.close()
#             self.__cursorHandle = None
#         if self.__connectionHandle:
#             self.__connectionHandle.close()
#             self.__connectionHandle = None

#     def commit(self, query: str, *args: Any) -> None:
#         if not self.__cursorHandle or not self.__connectionHandle:
#             raise RuntimeError("Database not connected!")

#         self.__commitCallback(query, args)