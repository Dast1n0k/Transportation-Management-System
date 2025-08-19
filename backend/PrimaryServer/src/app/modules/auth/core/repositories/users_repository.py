import inject
from sqlalchemy import select
from app.shared.database.common import OrmAsyncDatabaseHandler
from ..models import User

class UsersRepository:

    @inject.autoparams()
    def __init__(self, database: OrmAsyncDatabaseHandler) -> None:
        self.__database = database
        self.__database.connect()

    async def create(self, username: str, password: str, role: str) -> User:
        new_user = User(username=username, password=password, role=role)
        async with self.__database.getSession() as session:
            try:
                session.add(new_user)
                await session.commit()
                return new_user
            except Exception as error:
                await session.rollback()
                raise RuntimeError(
                    f"Failed to create user: {error}") from error

    async def get_all(self) -> list[User]:
        async with self.__database.getSession() as session:
            try:
                stmt = select(User)
                result = await session.execute(stmt)
                users = result.scalars().all()
                return list(users)
            except Exception as error:
                raise RuntimeError(
                    f"Failed to get all users: {error}") from error
