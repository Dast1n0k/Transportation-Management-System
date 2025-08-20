from typing import Any
from ...common import DatabaseConfig

class OrmSessionConfig(DatabaseConfig):

    def __init__(
        self,
        autoFlush: bool,
        autocommit: bool,
        expireOnCommit: bool
    ) -> None:
        self.__autoFlush = autoFlush
        self.__autocommit = autocommit
        self.__expireOnCommit = expireOnCommit

    def toDictionary(self) -> dict[str, Any]:
        return {
            "autoflush": self.__autoFlush,
            "autocommit": self.__autocommit,
            "expire_on_commit": self.__expireOnCommit,
        }
