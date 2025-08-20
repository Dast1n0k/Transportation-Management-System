from abc import ABC, abstractmethod
from typing import Any

class DatabaseConfig(ABC):
    @abstractmethod
    def toDictionary(self) -> dict[str, Any]: ...
