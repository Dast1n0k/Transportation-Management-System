import asyncio
import inject
from app.shared.database.common import OrmAsyncDatabaseHandler
from app.modules.auth.core.repositories import UsersRepository
from app.modules.auth.container import setup

async def main():
    # Make sure injection is configured once in your app entrypoint
    # (you already have inject.configure(setup) somewhere)

    inject.configure(setup)

    # You can let `inject` handle dependencies automatically
    users_repo = UsersRepository()

    # Create a new user
    new_user = await users_repo.create(
        username="mark",
        password="check",
        role="admin"
    )
    print(f"Created user: {new_user.id}, {new_user.username}")

    # Fetch all users
    users = await users_repo.get_all()
    print("All users:")
    for user in users:
        print(user.id, user.username, user.role)


if __name__ == "__main__":
    asyncio.run(main())
