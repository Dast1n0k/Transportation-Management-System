import asyncio
from app.modules.auth.core.repositories import UserRepository
from app.modules.auth.container import AuthContainer

async def main():
    # Make sure injection is configured once in your app entrypoint
    # (you already have inject.configure(setup) somewhere)

    authContainer = AuthContainer()
    authContainer.wire(modules = ["app.modules.auth.core.repositories.user_repository"])

    users_repo = UserRepository()

    # Create a new user
    new_user = await users_repo.create(
        username="testsubkect",
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
