from typing import Self
from .orm_engine_config import OrmEngineConfig

class OrmEngineConfigBuilder:

    def __init__(self) -> None:
        self.__echo = False
        self.__poolSize = 5
        self.__maxOverflow = 10
        self.__poolTimeout = 30
        self.__poolRecycle = 3600
        self.__poolPrePing = True

    def withEcho(self, echo: bool) -> Self:
        self.__echo = echo
        return self

    def withPoolSize(self, poolSize: int) -> Self:
        self.__poolSize = poolSize
        return self

    def withMaxOverflow(self, maxOverflow: int) -> Self:
        self.__maxOverflow = maxOverflow
        return self

    def withPoolTimeout(self, poolTimeout: int) -> Self:
        self.__poolTimeout = poolTimeout
        return self

    def withPoolRecycle(self, poolRecycle: int) -> Self:
        self.__poolRecycle = poolRecycle
        return self

    def withPoolPrePing(self, poolPrePing: bool) -> Self:
        self.__poolPrePing = poolPrePing
        return self

    def build(self) -> OrmEngineConfig:
        return OrmEngineConfig(
            self.__echo,
            self.__poolSize,
            self.__maxOverflow,
            self.__poolTimeout,
            self.__poolRecycle,
            self.__poolPrePing
        )
