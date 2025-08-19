import inject
from app.shared.database.config import OrmEngineConfigBuilder, OrmSessionConfigBuilder
from app.shared.database.common import OrmAsyncDatabaseHandler
from app.shared.database.concrete import SQLalchemyAsyncDatabaseHandler

def setup(binder):
    binder.bind(
        OrmAsyncDatabaseHandler,
        SQLalchemyAsyncDatabaseHandler(
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
    )