from typing import Any
from ...common import DatabaseConfig

class OrmEngineConfig(DatabaseConfig):

    def __init__(
        self,
        echo: bool,
        poolSize: int,
        maxOverflow: int,
        poolTimeout: int,
        poolRecycle: int,
        poolPrePing: bool
    ) -> None:
        self.__echo = echo
        self.__poolSize = poolSize
        self.__maxOverflow = maxOverflow
        self.__poolTimeout = poolTimeout
        self.__poolRecycle = poolRecycle
        self.__poolPrePing = poolPrePing

    def toDictionary(self) -> dict[str, Any]:
        return {
            "echo": self.__echo,
            "pool_size": self.__poolSize,
            "max_overflow": self.__maxOverflow,
            "pool_timeout": self.__poolTimeout,
            "pool_recycle": self.__poolRecycle,
            "pool_pre_ping": self.__poolPrePing
        }
