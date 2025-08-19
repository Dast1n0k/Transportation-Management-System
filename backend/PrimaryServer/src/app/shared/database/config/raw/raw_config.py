from typing import Any
from ..base.database_config import DatabaseConfig

class RawConfig(DatabaseConfig):

    def __init__(self, autoFlush: bool) -> None:
        self.__autoFlush = autoFlush

    def toDictionary(self) -> dict[str, Any]:
        return {
            "auto_flush": self.__autoFlush
        }
