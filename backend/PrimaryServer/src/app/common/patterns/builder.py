from typing import Any, Protocol

class BuilderProtocol(Protocol):
    def build(self) -> Any: ...
