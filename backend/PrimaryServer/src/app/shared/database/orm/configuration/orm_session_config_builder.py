from typing import Self
from .orm_session_config import OrmSessionConfig

class OrmSessionConfigBuilder:

    def __init__(self) -> None:
        self.__autoFlush = False
        self.__autocommit = False
        self.__expireOnCommit = False

    def withAutoFlush(self, autoFlush: bool) -> Self:
        self.__autoFlush = autoFlush
        return self

    def withAutocommit(self, autocommit: bool) -> Self:
        self.__autocommit = autocommit
        return self

    def withExpireOnCommit(self, expireOnCommit: bool) -> Self:
        self.__expireOnCommit = expireOnCommit
        return self

    def build(self) -> OrmSessionConfig:
        return OrmSessionConfig(
            self.__autoFlush,
            self.__autocommit,
            self.__expireOnCommit
        )
