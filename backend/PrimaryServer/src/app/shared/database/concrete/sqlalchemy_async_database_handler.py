from typing import (
    Any,
    Optional,
    AsyncIterator,
)

from contextlib import asynccontextmanager

from sqlalchemy import (
    text as sql_text,
    Result as SQLResult,
)
from sqlalchemy.exc import SQLAlchemyError
from sqlalchemy.ext.asyncio import (
    create_async_engine,
    AsyncEngine,
    AsyncSession,
    async_sessionmaker as AsyncSessionMaker,
)

from ..config import DatabaseConfig
from ..common import OrmAsyncDatabaseHandler

class SQLalchemyAsyncDatabaseHandler(OrmAsyncDatabaseHandler):

    def __init__(
        self,
        uri: str,
        engineConfig: DatabaseConfig,
        sessionConfig: DatabaseConfig
    ) -> None:
        super().__init__(uri)
        self.__engineConfig = engineConfig
        self.__sessionConfig = sessionConfig
        self.__engine: Optional[AsyncEngine] = None
        self.__sessionFactory: Optional[AsyncSessionMaker] = None

    @asynccontextmanager
    async def getSession(self) -> AsyncIterator[AsyncSession]:
        if not self.__sessionFactory:
            raise RuntimeError(f"Database is not connected. Call {self.connect.__name__}() first.")

        async with self.__sessionFactory() as session:
            try:
                yield session
            except SQLAlchemyError as error:
                await session.rollback()
                raise RuntimeError(f"Failed to get session: {error}") from error
            finally:
                await session.close()

    def connect(self):
        try:
            self.__engine = create_async_engine(
                self._uri,
                **self.__engineConfig.toDictionary()
            )
            self.__sessionFactory = AsyncSessionMaker(
                self.__engine,
                **self.__sessionConfig.toDictionary()
            )
        except SQLAlchemyError as error:
            self.__dispose()
            raise RuntimeError(f"Failed to connect to database: {error}")

    async def execute(self, query: str, *args: Any) -> SQLResult:
        if not self._session_factory:
            raise RuntimeError(f"Database is not connected. Call {self.connect.__name__}() first.")

        async with self.getSession() as session:
            try:
                if args:
                    result = await session.execute(sql_text(query), args)
                else:
                    result = await session.execute(sql_text(query))
                await session.commit()
                return result
            except SQLAlchemyError as error:
                await session.rollback()
                raise RuntimeError(f"Query execution failed: {error}") from error

    def disconnect(self):
        self.__dispose()

    def __dispose(self) -> None:
        try:
            if self.__engine:
                self.__engine.dispose()
                self.__engine = None
                self.__sessionFactory = None
        except SQLAlchemyError as error:
            raise RuntimeError(f"Failed to disconnect from database: {error}") from error
