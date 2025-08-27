
from flask import current_app
import sqlite3


def get_db():
    conn = sqlite3.connect(current_app.config["DATABASE_AUTH"])
    conn.row_factory = sqlite3.Row
    return conn
