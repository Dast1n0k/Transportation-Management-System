from dependency_injector import containers, providers
from app.shared.database.orm import OrmEngineConfigBuilder, OrmSessionConfigBuilder
from app.shared.database.orm import SQLalchemyAsyncDatabaseHandler

class AuthContainer(containers.DeclarativeContainer):
    AuthDatabase = providers.Singleton(
        SQLalchemyAsyncDatabaseHandler,
        "sqlite+aiosqlite:///dbs/auth.db",
        OrmEngineConfigBuilder()
            .withEcho(True)
            .withPoolSize(1)
            .withMaxOverflow(0)
            .withPoolTimeout(30)
            .withPoolRecycle(-1)
            .withPoolPrePing(False)
            .build(),
        OrmSessionConfigBuilder()
            .withAutoFlush(False)
            .withAutocommit(False)
            .withExpireOnCommit(False)
            .build()
    )
