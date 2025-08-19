from abc import ABC, abstractmethod
from ...models.orm import User

class UserServiceABC(ABC):

    @abstractmethod
    def getUser(self, userId: int) -> User | None: ...
